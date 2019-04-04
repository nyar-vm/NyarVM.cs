using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Clr.Encode;

/// <summary>
///     CLR 妯″潡缂栫爜鍣紝鐨凜lrModuleData 缂栫爜鐨?NET 绋嬪簭闆嗭紙PE + CLR 鍏冩暟鐨? MSIL锛夌殑
/// </summary>
public sealed class ClrEncoder
{
    /// <summary>
    ///     鐨凜LR 妯″潡鏁版嵁缂栫爜涓哄瓧鑺傛暟缁勭殑
    /// </summary>
    public byte[] encode(ClrModuleData module)
    {
        return ManagedPeBuilderAdapter.build(module);
    }

    /// <summary>
    ///     浠呯紪鐨凪SIL 鎸囦护搴忓垪涓哄瓧鑺傛暟缁勭殑
    /// </summary>
    public static byte[] encode_instructions(IReadOnlyList<ClrInstruction> instructions)
    {
        var size = estimate_instruction_size(instructions);
        var writer = new ByteBufferWriter(size);

        foreach (var instr in instructions) write_instruction(ref writer, instr);

        return writer.to_array();
    }

    /// <summary>
    ///     缂栫爜鏂规硶浣擄紙Fat 鐨? MSIL + 寮傚父澶勭悊琛級鐨?
    /// </summary>
    public static byte[] encode_method_body(IReadOnlyList<ClrInstruction> instructions, ushort maxStack = 8,
        uint localVarSigTok = 0, IReadOnlyList<ClrExceptionHandler>? exceptionHandlers = null,
        bool initLocals = true)
    {
        var codeBytes = encode_instructions(instructions);
        var codeSize = codeBytes.Length;
        var hasEh = exceptionHandlers is { Count: > 0 };

        if (codeSize < 64 && !hasEh && localVarSigTok == 0 && maxStack <= 8)
        {
            var tinyWriter = new ByteBufferWriter(1 + codeSize);
            tinyWriter.write_u8((byte)((codeSize << 2) | ClrConstants.method_header_tiny_flag));
            tinyWriter.write(codeBytes);

            return tinyWriter.to_array();
        }

        // Fat 鏂规硶澶寸殑 Size 浣嶄綅浜庨珮 4 bit锛堜互 DWORD 璁★級锛岃〃绀?12 瀛楄妭澶淬€?
        var headerFlags = (ushort)(ClrConstants.method_header_fat_flag | 0x3000);

        // 鍙獙璇佹柟娉曚腑鏈夊眬閮ㄥ彉閲忔椂蹇呴』璁剧疆 InitLocals 鏍囧織
        if (initLocals)
        {
            headerFlags |= ClrConstants.method_header_init_locals;
        }

        if (hasEh) headerFlags |= ClrConstants.method_header_more_sects;

        var ehBytes = hasEh ? encode_exception_handlers(exceptionHandlers!) : [];

        var fatWriter = new ByteBufferWriter(12 + codeSize + ehBytes.Length);
        fatWriter.write_u16_le(headerFlags);
        fatWriter.write_u16_le(maxStack);
        fatWriter.write_u32_le((uint)codeSize);
        fatWriter.write_u32_le(localVarSigTok);
        fatWriter.write(codeBytes);

        if (ehBytes.Length > 0)
        {
            var padding = (4 - codeSize % 4) % 4;

            for (var i = 0; i < padding; i++) fatWriter.write_u8(0);

            fatWriter.write(ehBytes);
        }

        return fatWriter.to_array();
    }

    #region 寮傚父澶勭悊琛ㄧ紪鐨?

    /// <summary>
    ///     缂栫爜寮傚父澶勭悊琛紙Fat 鏍煎紡锛夌殑
    /// </summary>
    private static byte[] encode_exception_handlers(IReadOnlyList<ClrExceptionHandler> handlers)
    {
        var dataSize = 4 + handlers.Count * 24;
        var writer = new ByteBufferWriter(dataSize + 4);

        writer.write_u8(ClrConstants.exception_handler_table_flag | ClrConstants.exception_handler_fat_flag);
        writer.write_u8((byte)(dataSize >> 16));
        writer.write_u8((byte)(dataSize >> 8));
        writer.write_u8((byte)dataSize);

        foreach (var eh in handlers)
        {
            writer.write_u32_le((uint)eh.handler_kind);
            writer.write_u32_le(eh.try_start);
            writer.write_u32_le(eh.try_length);
            writer.write_u32_le(eh.handler_start);
            writer.write_u32_le(eh.handler_length);
            writer.write_u32_le(eh.class_token_or_filter_offset);
        }

        return writer.to_array();
    }

    #endregion

    #region ManagedPE 鐢熸垚

    /// <summary>
    ///     鍩轰簬 BCL 鐨凪anagedPEBuilder 鐢熸垚鍙墽鐨凜LR 绋嬪簭闆嗭紝閬垮厤鎵嬪啓 PE 缁嗚妭宸紓鐨?
    /// </summary>
    private static class ManagedPeBuilderAdapter
    {
        public static byte[] build(ClrModuleData module)
        {
            var metadata = new MetadataBuilder();
            var ilBuilder = new BlobBuilder();

            var moduleFileName = string.IsNullOrWhiteSpace(module.module_name) ? "Module.exe" : module.module_name;
            var assemblyName = Path.GetFileNameWithoutExtension(moduleFileName);
            if (string.IsNullOrWhiteSpace(assemblyName)) assemblyName = "Module";

            var mvid = resolve_mvid(module);
            metadata.AddModule(
                0,
                metadata.GetOrAddString(moduleFileName),
                metadata.GetOrAddGuid(mvid),
                default,
                default);

            metadata.AddAssembly(
                metadata.GetOrAddString(assemblyName),
                parse_assembly_version(module.version),
                default,
                default,
                default,
                AssemblyHashAlgorithm.Sha1);

            var (externalMemberRefTokenMap, actualMemberRefTokens, sharedTypeRefHandles, sharedAssemblyRefHandles) =
                emit_external_refs(metadata, module.external_method_refs, module.target_runtime_version);

            // 涓哄閮ㄧ被鍨嬪紩鐢ㄧ敓鎴?TypeRef 鍏冩暟鎹紝骞舵瀯寤轰护鐗岄噸鏄犲皠
            // 鍏变韩 emit_external_refs 鍒涘缓鐨?TypeRef 琛紝閬垮厤閲嶅鍒涘缓鐩稿悓鐨?TypeRef 琛屽鑷寸储寮曞亸绉伙紝浠庤€屼繚璇?0x0100000B 绫诲瀷浠ょ墝涔熻兘鎵惧埌鏄犲皠
            var typeRefRemap = emit_external_type_refs(metadata, module.external_type_refs, module.target_runtime_version, sharedTypeRefHandles, sharedAssemblyRefHandles);

            // 涓烘墍鏈夌敤鎴峰畾涔夌被鍨嬪垱寤?System.Object 鍩虹被寮曠敤
            // .NET Core / 8.0 涓?System.Object 浣嶄簬 System.Runtime 绋嬪簭闆?
            var systemRuntimePublicKeyToken = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };
            var systemObjectAssemblyRef = metadata.AddAssemblyReference(
                metadata.GetOrAddString("System.Runtime"),
                module.target_runtime_version.Major > 0
                    ? module.target_runtime_version
                    : new Version(10, 0, 0, 0),
                default,
                metadata.GetOrAddBlob(systemRuntimePublicKeyToken),
                default,
                default);
            var systemObjectTypeRef = metadata.AddTypeReference(
                systemObjectAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            // 娉ㄥ唽鐢ㄦ埛瀛楃涓插苟鎹曡幏 MetadataBuilder 鍒嗛厤鐨勫疄闄?Token锛堝熀浜?#US 鍫嗗亸绉伙級
            var userStringHandles = new List<UserStringHandle>();
            foreach (var userString in module.user_strings)
                userStringHandles.Add(metadata.GetOrAddUserString(userString));

            // 鏋勫缓 Token 閲嶆槧灏勫瓧鍏革細ClrBackend 棰勮绠?Token 鈫?瀹為檯 Metadata Token
            var userStringRemap = build_user_string_remap(userStringHandles);
            var memberRefRemap = build_member_ref_remap(actualMemberRefTokens);

            // DEBUG: 鎵撳嵃鐢ㄦ埛瀛楃涓?Token 閲嶆槧灏勪俊鎭?

            var methods = collect_method_definitions(module);
            var fields = module.types.SelectMany(t => t.fields).ToList();

            // 鏋勫缓 FieldDef Token 閲嶆槧灏勮〃锛欳lrBackend 棰勮绠?Token 鈫?瀹為檯 Metadata Token
            // ClrBackend 鐨?field_token_map 浣跨敤 "TypeName.fieldName" 浣滀负閿紝
            // 鑰?ClrEncoder 鎸?module.types.SelectMany(t => t.fields) 椤哄簭鍒嗛厤 Token銆?
            // 褰?ClrBackend 鐨?currentType 琚摝闄や负 external_ref 鏃讹紝浼氫骇鐢?external_ref.* 閿紝
            // 杩欎簺閿棤娉曠洿鎺ュ尮閰嶅疄闄呯被鍨嬶紝闇€瑕侀€氳繃瀛楁鍚嶅洖閫€鍖归厤銆?
            var fieldDefRemap = build_field_def_remap(module.types, module.field_token_map);

            // 璇婃柇锛氭墦鍗板瓧娈靛畾涔夌殑鍒嗛厤椤哄簭涓庡疄闄?Token
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var fieldToken = 0x04000001u + (uint)i;
            }

            foreach (var field in fields)
            {
                var signature = get_field_signature(field);
                metadata.AddFieldDefinition(
                    (FieldAttributes)field.flags,
                    metadata.GetOrAddString(field.name),
                    metadata.GetOrAddBlob(signature));
            }

            var firstParameterHandle = MetadataTokens.ParameterHandle(1);

            // 瀵规墍鏈夋柟娉曚綋鐨勬寚浠よ繘琛?Token 閲嶆槧灏勶紝纭繚鎸囦护涓殑 Token 涓庡厓鏁版嵁琛ㄤ竴鑷?
            foreach (var method in methods)
            {
                remap_instruction_tokens(method.instructions, userStringRemap, memberRefRemap, typeRefRemap, fieldDefRemap, method.name);
            }

            foreach (var method in methods)
            {
                var localVarSigTok = method.local_variable_types.Count > 0
                    ? (uint)MetadataTokens.GetToken(
                        metadata.AddStandaloneSignature(
                            metadata.GetOrAddBlob(build_local_variable_signature(method.local_variable_types))))
                    : method.local_var_sig_tok;
                var methodBodyBytes = encode_method_body(
                    method.instructions,
                    method.max_stack,
                    localVarSigTok,
                    method.exception_handlers,
                    method.init_locals);
                var bodyOffset = ilBuilder.Count;
                ilBuilder.WriteBytes(methodBodyBytes);

                while ((ilBuilder.Count & 3) != 0) ilBuilder.WriteByte(0);

                var signature = method.signature.Length == 0 ? [0x00, 0x00, 0x01] : method.signature;
                metadata.AddMethodDefinition(
                    (MethodAttributes)method.flags,
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    metadata.GetOrAddString(method.name),
                    metadata.GetOrAddBlob(signature),
                    bodyOffset,
                    firstParameterHandle);
            }

            var firstFieldHandle = MetadataTokens.FieldDefinitionHandle(1);
            var methodStart = 1;
            var fieldStart = 1;
            foreach (var type in module.types)
            {
                // ECMA-335: <Module> 绫诲瀷鐨?Extends 蹇呴』涓?0
                var baseType = string.Equals(type.name, "<Module>", StringComparison.Ordinal)
                    ? default(EntityHandle)
                    : systemObjectTypeRef;

                metadata.AddTypeDefinition(
                    (TypeAttributes)type.flags,
                    metadata.GetOrAddString(type.@namespace ?? string.Empty),
                    metadata.GetOrAddString(type.name),
                    baseType,
                    MetadataTokens.FieldDefinitionHandle(fieldStart),
                    MetadataTokens.MethodDefinitionHandle(methodStart));

                fieldStart += type.fields.Count;
                methodStart += type.methods.Count;
            }

            var entryPointHandle = resolve_entry_point_handle(module, methods, externalMemberRefTokenMap);

            var peBuilder = new ManagedPEBuilder(
                new PEHeaderBuilder(
                    imageCharacteristics: entryPointHandle.IsNil
                        ? Characteristics.Dll
                        : Characteristics.ExecutableImage,
                    subsystem: Subsystem.WindowsCui,
                    dllCharacteristics: DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible |
                                        DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware),
                new MetadataRootBuilder(metadata),
                ilBuilder,
                null,
                null,
                strongNameSignatureSize: 0,
                entryPoint: entryPointHandle,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: null);

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            return peBlob.ToArray();
        }

        private static Guid resolve_mvid(ClrModuleData module)
        {
            var raw = module.metadata.guid_heap.data;
            if (raw.Length >= 16) return new Guid(raw.AsSpan(0, 16));

            return Guid.NewGuid();
        }

        /// <summary>
        ///     收集真实写入 `MethodDef` 表的方法顺序。
        ///     `module.methods` 保存 `<Module>` 上的方法，用户类型上的方法需要追加在后面，
        ///     但不能再把 `<Module>` 自己的方法重复拼一遍。
        /// </summary>
        private static List<ClrMethodDef> collect_method_definitions(ClrModuleData module)
        {
            if (module.methods.Count == 0)
            {
                return module.types.SelectMany(t => t.methods).ToList();
            }

            var methods = module.methods.ToList();
            foreach (var type in module.types)
            {
                if (string.Equals(type.name, "<Module>", StringComparison.Ordinal))
                {
                    continue;
                }

                methods.AddRange(type.methods);
            }

            return methods;
        }

        private static Version parse_assembly_version(string? version)
        {
            return Version.TryParse(version, out var parsed)
                ? new Version(
                    System.Math.Max(0, parsed.Major),
                    System.Math.Max(0, parsed.Minor),
                    System.Math.Max(0, parsed.Build),
                    System.Math.Max(0, parsed.Revision))
                : new Version(1, 0, 0, 0);
        }

        private static byte[] build_local_variable_signature(IReadOnlyList<string> localTypes)
        {
            var signature = new List<byte>(4 + localTypes.Count)
            {
                0x07
            };

            append_compressed_unsigned(signature, (uint)localTypes.Count);
            foreach (var localType in localTypes) signature.Add(map_element_type(localType));

            return [.. signature];
        }

        private static void append_compressed_unsigned(ICollection<byte> buffer, uint value)
        {
            if (value <= 0x7F)
            {
                buffer.Add((byte)value);
                return;
            }

            if (value <= 0x3FFF)
            {
                buffer.Add((byte)((value >> 8) | 0x80));
                buffer.Add((byte)(value & 0xFF));
                return;
            }

            buffer.Add((byte)((value >> 24) | 0xC0));
            buffer.Add((byte)((value >> 16) & 0xFF));
            buffer.Add((byte)((value >> 8) & 0xFF));
            buffer.Add((byte)(value & 0xFF));
        }

        private static byte map_element_type(string typeName)
        {
            return typeName.ToLowerInvariant() switch
            {
                "void" => 0x01,
                "bool" => 0x02,
                "i8" => 0x04,
                "i16" => 0x06,
                "i32" => 0x08,
                "i64" => 0x0A,
                "f32" => 0x0C,
                "f64" => 0x0D,
                "string" => 0x0E,
                "utf8" => 0x0E,
                _ => 0x1C
            };
        }

        private static byte[] get_field_signature(ClrFieldDef field)
        {
            // `0x06` 涓?FIELD 绛惧悕璋冪敤绾﹀畾锛宍0x1C` 涓?`ELEMENT_TYPE_OBJECT`銆?
            // 鐢ㄦ埛 OOP 绫诲瀷鐨勫瓧娈靛潎涓?object 绫诲瀷銆?
            return [0x06, 0x1C];
        }


        /// <summary>
        ///     涓烘墍鏈夊閮ㄦ柟娉曞紩鐢ㄧ敓鐨凙ssemblyRef 鐨凾ypeRef 鐨凪emberRef 鍏冩暟鎹潯鐩殑
        ///     杩斿洖 MemberRef token 鍊煎埌 MemberReferenceHandle 鐨勬槧灏勶紝浠ュ強鎸夐『搴忕殑瀹為檯 Token 鍒楄〃鐨?
        /// </summary>
        private static (Dictionary<uint, MemberReferenceHandle> TokenMap, List<uint> ActualTokens, Dictionary<(string, string, string), TypeReferenceHandle> TypeRefHandles, Dictionary<string, AssemblyReferenceHandle> AssemblyRefHandles) emit_external_refs(
            MetadataBuilder metadata,
            IReadOnlyList<ClrExternalMethodRef> externalRefs,
            Version TargetSpecificationVersion)
        {
            var memberRefTokenMap = new Dictionary<uint, MemberReferenceHandle>();
            var actualTokens = new List<uint>(externalRefs.Count);
            var assemblyRefHandles = new Dictionary<string, AssemblyReferenceHandle>(StringComparer.Ordinal);
            var typeRefHandles = new Dictionary<(string, string, string), TypeReferenceHandle>();

            // Public key tokens for the framework assemblies we currently emit references for.
            var systemRuntimePublicKeyToken = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };
            var mscorlibPublicKeyToken = new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };

            foreach (var extRef in externalRefs)
            {
                if (!assemblyRefHandles.TryGetValue(extRef.assembly_name, out var assemblyRefHandle))
                {
                    var version = extRef.assembly_name switch
                    {
                        "mscorlib" => new Version(4, 0, 0, 0),
                        { } name when name.StartsWith("System.", StringComparison.Ordinal) => TargetSpecificationVersion,
                        _ => new Version(0, 0, 0, 0)
                    };
                    var publicKeyOrToken = extRef.assembly_name switch
                    {
                        "mscorlib" => metadata.GetOrAddBlob(mscorlibPublicKeyToken),
                        { } name when name.StartsWith("System.", StringComparison.Ordinal) =>
                            metadata.GetOrAddBlob(systemRuntimePublicKeyToken),
                        _ => default
                    };

                    assemblyRefHandle = metadata.AddAssemblyReference(
                        metadata.GetOrAddString(extRef.assembly_name),
                        version,
                        default,
                        publicKeyOrToken,
                        default,
                        default);
                    assemblyRefHandles[extRef.assembly_name] = assemblyRefHandle;
                }

                var typeKey = (AssemblyName: extRef.assembly_name, TypeNamespace: extRef.type_namespace,
                    TypeName: extRef.type_name);
                if (!typeRefHandles.TryGetValue(typeKey, out var typeRefHandle))
                {
                    typeRefHandle = metadata.AddTypeReference(
                        assemblyRefHandle,
                        metadata.GetOrAddString(extRef.type_namespace),
                        metadata.GetOrAddString(extRef.type_name));
                    typeRefHandles[typeKey] = typeRefHandle;
                }

                // 妫€娴嬫硾鍨嬬被鍨嬶紙绫诲瀷鍚嶅寘鍚?` 瀛楃锛夊苟鍒涘缓 TypeSpec 瀹炰緥鍖?
                // 渚嬪 List`1 鈫?List`1<object>
                EntityHandle memberRefParent = typeRefHandle;
                if (extRef.type_name.Contains('`'))
                {
                    var typeSpecHandle = create_generic_type_spec(metadata, typeRefHandle, extRef.type_name);
                    memberRefParent = typeSpecHandle;
                }

                var signature = extRef.method_signature.Length == 0
                    ? [0x00, 0x00, 0x01]
                    : extRef.method_signature;
                var memberRefHandle = metadata.AddMemberReference(
                    memberRefParent,
                    metadata.GetOrAddString(extRef.method_name),
                    metadata.GetOrAddBlob(signature));

                var token = (uint)MetadataTokens.GetToken(memberRefHandle);
                memberRefTokenMap[token] = memberRefHandle;
                actualTokens.Add(token);
            }

            return (memberRefTokenMap, actualTokens, typeRefHandles, assemblyRefHandles);
        }

        /// <summary>
        ///     涓烘硾鍨嬬被鍨嬪垱寤?TypeSpec 鍏冩暟鎹潯鐩紝灏嗘墍鏈夋硾鍨嬪弬鏁板疄渚嬪寲涓?object銆?
        ///     渚嬪 List`1 鈫?List`1&lt;object&gt;銆?
        /// </summary>
        /// <param name="metadata">鍏冩暟鎹瀯寤哄櫒銆?/param>
        /// <param name="typeRefHandle">鏈疄渚嬪寲鐨勬硾鍨嬬被鍨?TypeRef 鍙ユ焺銆?/param>
        /// <param name="typeName">绫诲瀷鍚嶏紙濡?"List`1"锛夈€?/param>
        /// <returns>TypeSpec 鍙ユ焺銆?/returns>
        private static TypeSpecificationHandle create_generic_type_spec(
            MetadataBuilder metadata,
            TypeReferenceHandle typeRefHandle,
            string typeName)
        {
            // 浠庣被鍨嬪悕涓彁鍙栨硾鍨嬪弬鏁版暟閲忥紙渚嬪 "List`1" 鈫?1锛?
            var backtickIndex = typeName.IndexOf('`');
            var genericParamCount = int.Parse(typeName[(backtickIndex + 1)..]);

            // 浣跨敤 BlobBuilder 鐩存帴鏋勫缓 TypeSpec 绛惧悕
            var typeSpecSig = new BlobBuilder();

            // GENERICINST (0x15)
            typeSpecSig.WriteByte(0x15);

            // GenericType: ELEMENT_TYPE_CLASS (0x12) + TypeDefOrRef 缂栫爜鐨勪护鐗?
            typeSpecSig.WriteByte(0x12);
            var typeRefRow = MetadataTokens.GetRowNumber(typeRefHandle);
            var encodedTypeRef = ((uint)typeRefRow << 2) | 1; // TypeRef 缂栫爜: (row << 2) | 1
            write_compressed_unsigned_to_blob(typeSpecSig, encodedTypeRef);

            // GenArgCount
            write_compressed_unsigned_to_blob(typeSpecSig, (uint)genericParamCount);

            // 娉涘瀷鍙傛暟锛氬叏閮ㄥ疄渚嬪寲涓?object锛圗LEMENT_TYPE_OBJECT = 0x1C锛?
            for (var i = 0; i < genericParamCount; i++)
            {
                typeSpecSig.WriteByte(0x1C);
            }

            return metadata.AddTypeSpecification(metadata.GetOrAddBlob(typeSpecSig));
        }

        /// <summary>
        ///     灏嗗帇缂╂棤绗﹀彿鏁存暟鍐欏叆 BlobBuilder銆?
        /// </summary>
        private static void write_compressed_unsigned_to_blob(BlobBuilder builder, uint value)
        {
            if (value <= 0x7F)
            {
                builder.WriteByte((byte)value);
            }
            else if (value <= 0x3FFF)
            {
                builder.WriteByte((byte)((value >> 8) | 0x80));
                builder.WriteByte((byte)(value & 0xFF));
            }
            else
            {
                builder.WriteByte((byte)((value >> 24) | 0xC0));
                builder.WriteByte((byte)((value >> 16) & 0xFF));
                builder.WriteByte((byte)((value >> 8) & 0xFF));
                builder.WriteByte((byte)(value & 0xFF));
            }
        }


        /// <summary>
        ///     鏋勫缓鐢ㄦ埛瀛楃涓?Token 閲嶆槧灏勫瓧鍏革細ClrBackend 鐨勭储寮?Token锛?x70000001 + i锛夆啋 瀹為檯 #US 鍫嗗亸绉?Token
        /// </summary>
        private static Dictionary<uint, uint> build_user_string_remap(List<UserStringHandle> handles)
        {
            var remap = new Dictionary<uint, uint>();
            for (var i = 0; i < handles.Count; i++)
            {
                var handle = handles[i];
                var oldToken = 0x70000001u + (uint)i;
                var newToken = (uint)MetadataTokens.GetToken(handle);
                remap[oldToken] = newToken;
            }

            return remap;
        }

        /// <summary>
        ///     鏋勫缓 MemberRef Token 閲嶆槧灏勫瓧鍏革細ClrBackend 鐨勭储寮?Token锛?x0A000001 + i锛夆啋 瀹為檯 Token
        /// </summary>
        private static Dictionary<uint, uint> build_member_ref_remap(List<uint> actualTokens)
        {
            var remap = new Dictionary<uint, uint>();
            for (var i = 0; i < actualTokens.Count; i++)
            {
                var oldToken = 0x0A000001u + (uint)i;
                remap[oldToken] = actualTokens[i];
            }

            return remap;
        }

        /// <summary>
        ///     涓烘墍鏈夊閮ㄧ被鍨嬪紩鐢ㄧ敓鎴?TypeRef 鍏冩暟鎹潯鐩紝骞舵瀯寤轰护鐗岄噸鏄犲皠瀛楀吀銆?
        ///     ClrBackend 鐨勭储寮?Token锛?x01000001 + i锛夆啋 瀹為檯 TypeRef Token
        /// </summary>
        private static Dictionary<uint, uint> emit_external_type_refs(
            MetadataBuilder metadata,
            IReadOnlyList<ClrExternalTypeRef> externalTypeRefs,
            Version targetRuntimeVersion,
            Dictionary<(string, string, string), TypeReferenceHandle> existingTypeRefHandles,
            Dictionary<string, AssemblyReferenceHandle> existingAssemblyRefHandles)
        {
            var remap = new Dictionary<uint, uint>();
            if (externalTypeRefs.Count == 0)
            {
                return remap;
            }

            var assemblyRefHandles = existingAssemblyRefHandles;
            var systemRuntimePublicKeyToken = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };

            for (var i = 0; i < externalTypeRefs.Count; i++)
            {
                var typeRef = externalTypeRefs[i];

                if (!assemblyRefHandles.TryGetValue(typeRef.assembly_name, out var assemblyRefHandle))
                {
                    var version = typeRef.assembly_name.StartsWith("System.", StringComparison.Ordinal)
                        ? targetRuntimeVersion
                        : new Version(0, 0, 0, 0);
                    var publicKeyOrToken = typeRef.assembly_name.StartsWith("System.", StringComparison.Ordinal)
                        ? metadata.GetOrAddBlob(systemRuntimePublicKeyToken)
                        : default;

                    assemblyRefHandle = metadata.AddAssemblyReference(
                        metadata.GetOrAddString(typeRef.assembly_name),
                        version,
                        default,
                        publicKeyOrToken,
                        default,
                        default);
                    assemblyRefHandles[typeRef.assembly_name] = assemblyRefHandle;
                }

                var typeKey = (typeRef.assembly_name, typeRef.type_namespace, typeRef.type_name);
                TypeReferenceHandle typeRefHandle;
                if (existingTypeRefHandles.TryGetValue(typeKey, out var existingHandle))
                {
                    typeRefHandle = existingHandle;
                }
                else
                {
                    typeRefHandle = metadata.AddTypeReference(
                        assemblyRefHandle,
                        metadata.GetOrAddString(typeRef.type_namespace),
                        metadata.GetOrAddString(typeRef.type_name));
                    existingTypeRefHandles[typeKey] = typeRefHandle;
                }

                var oldToken = 0x01000001u + (uint)i;
                var newToken = (uint)MetadataTokens.GetToken(typeRefHandle);
                remap[oldToken] = newToken;
            }

            return remap;
        }

        /// <summary>
        ///     鏋勫缓 FieldDef Token 閲嶆槧灏勮〃銆?
        ///     ClrBackend 鐨?field_token_map 浣跨敤 "TypeName.fieldName" 浣滀负閿紝
        ///     鍏朵腑 TypeName 鍙兘鏄鎿﹂櫎绫诲瀷锛坋xternal_ref/any/object锛夛紝
        ///     姝ゆ椂闇€瑕侀€氳繃瀛楁鍚嶅洖閫€鍖归厤瀹為檯绫诲瀷銆?
        /// </summary>
        private static Dictionary<uint, uint> build_field_def_remap(
            IReadOnlyList<ClrTypeDef> types,
            IReadOnlyDictionary<string, uint> fieldTokenMap)
        {
            var remap = new Dictionary<uint, uint>();

            if (fieldTokenMap.Count == 0)
            {
                return remap;
            }

            // 1. 鏋勫缓 "TypeName.fieldName" 鈫?鏂癟oken 鐨勬槧灏勶紙鏉ヨ嚜瀹為檯绫诲瀷瀹氫箟锛?
            var qualifiedKeyToNewToken = new Dictionary<string, uint>(StringComparer.Ordinal);
            // 2. 鏋勫缓 "fieldName" 鈫?鏂癟oken 鍒楄〃 鐨勬槧灏勶紙鐢ㄤ簬鍥為€€鍖归厤锛?
            var fieldNameToNewTokens = new Dictionary<string, List<uint>>(StringComparer.Ordinal);

            var fieldIndex = 0u;
            foreach (var type in types)
            {
                // 璺宠繃 <Module> 绫诲瀷锛堟棤瀛楁锛?
                foreach (var field in type.fields)
                {
                    var newToken = 0x04000001u + fieldIndex;
                    var qualifiedKey = $"{type.name}.{field.name}";

                    qualifiedKeyToNewToken[qualifiedKey] = newToken;

                    if (!fieldNameToNewTokens.TryGetValue(field.name, out var list))
                    {
                        list = [];
                        fieldNameToNewTokens[field.name] = list;
                    }
                    list.Add(newToken);

                    fieldIndex++;
                }
            }

            // 3. 閬嶅巻 ClrBackend 鐨?field_token_map锛屾瀯寤?鏃oken 鈫?鏂癟oken 鏄犲皠
            foreach (var (qualifiedKey, oldToken) in fieldTokenMap)
            {
                // 鎯呭喌 A锛氶檺瀹氶敭鐩存帴鍖归厤瀹為檯绫诲瀷
                if (qualifiedKeyToNewToken.TryGetValue(qualifiedKey, out var newToken))
                {
                    if (newToken != oldToken)
                    {
                        remap[oldToken] = newToken;
                    }

                    continue;
                }

                // 鎯呭喌 B锛氶檺瀹氶敭鐨勭被鍨嬮儴鍒嗘槸琚摝闄ょ被鍨嬶紙external_ref/any/object锛夛紝
                // 閫氳繃瀛楁鍚嶅洖閫€鍖归厤銆備粎褰撳瓧娈靛悕鍞竴鏃舵墠鍖归厤銆?
                var lastDot = qualifiedKey.LastIndexOf('.');
                if (lastDot < 0)
                {
                    continue;
                }

                var fieldName = qualifiedKey.Substring(lastDot + 1);
                if (fieldNameToNewTokens.TryGetValue(fieldName, out var candidates) && candidates.Count == 1)
                {
                    var candidateToken = candidates[0];
                    if (candidateToken != oldToken)
                    {
                        remap[oldToken] = candidateToken;
                    }
                }
                else if (candidates is { Count: > 1 })
                {
                    // 瀛楁鍚嶄笉鍞竴锛屾棤娉曠‘瀹氬疄闄呯被鍨嬨€?
                    // 灏濊瘯鍚彂寮忥細閫夋嫨绗竴涓€欓€?Token锛堟寜绫诲瀷瀹氫箟椤哄簭锛夈€?
                    // 杩欏彲鑳藉湪缃曡鎯呭喌涓嬮€夋嫨閿欒鐨勫瓧娈碉紝浣嗕紭浜庢棤鏁?Token銆?
                    // 娉ㄦ剰锛氭鍥為€€浠呭湪 currentType 琚摝闄や笖瀛楁鍚嶆涔夋椂瑙﹀彂銆?
                    var heuristicToken = candidates[0];
                    if (heuristicToken != oldToken)
                    {
                        remap[oldToken] = heuristicToken;
                    }
                }
            }

            return remap;
        }

        /// <summary>
        ///     閲嶆槧灏勬寚浠ゅ垪琛ㄤ腑鐨?Token 鎿嶄綔鏁帮紝灏?ClrBackend 棰勮绠楃殑 Token 鏇挎崲涓哄疄闄呭厓鏁版嵁 Token
        /// </summary>
        private static void remap_instruction_tokens(IReadOnlyList<ClrInstruction> instructions,
            IReadOnlyDictionary<uint, uint> userStringRemap, IReadOnlyDictionary<uint, uint> memberRefRemap,
            IReadOnlyDictionary<uint, uint> typeRefRemap, IReadOnlyDictionary<uint, uint> fieldDefRemap,
            string methodName = "")
        {
            var remappedCount = 0;
            var failedCount = 0;
            foreach (var instr in instructions)
            {
                if (instr.operand is ClrTokenOperand tokenOperand)
                {
                    var oldToken = tokenOperand.value;
                    var tableType = oldToken >> 24;

                    switch (tableType)
                    {
                        case 0x01:
                        {
                            if (typeRefRemap.TryGetValue(oldToken, out var newTrToken))
                            {
                                tokenOperand.value = newTrToken;
                                remappedCount++;
                            }
                            else
                            {
                                failedCount++;
                            }

                            break;
                        }
                        case 0x04:
                        {
                            // FieldDef Token 閲嶆槧灏?
                            // 娉ㄦ剰锛氬綋 oldToken 宸茬粡绛変簬瀹為檯 Token 鏃讹紝涓嶉渶瑕侀噸鏄犲皠锛?
                            // 涔熶笉搴旇涓哄け璐ャ€?
                            if (fieldDefRemap.TryGetValue(oldToken, out var newFieldToken))
                            {
                                if (newFieldToken != oldToken)
                                {
                                    tokenOperand.value = newFieldToken;
                                    remappedCount++;
                                }
                            }
                            else
                            {
                                // Token 涓嶅湪閲嶆槧灏勮〃涓紝鍙兘宸茬粡鏄纭殑 Token锛堟棤闇€閲嶆槧灏勶級锛?
                                // 涔熷彲鑳芥槸鏃犳晥鐨?FieldDef Token銆?
                                // 浠呭湪 Token 瓒呭嚭瀹為檯瀛楁鏁伴噺鏃舵墠鎶ュ憡澶辫触銆?
                                // 鐢变簬姝ゅ鏃犳硶璁块棶瀛楁鎬绘暟锛屾殏涓嶆姤鍛婂け璐ャ€?
                            }

                            break;
                        }
                        case 0x70:
                        {
                            if (userStringRemap.TryGetValue(oldToken, out var newStrToken))
                            {
                                tokenOperand.value = newStrToken;
                                remappedCount++;
                            }
                            else
                            {
                                failedCount++;
                            }

                            break;
                        }
                        case 0x0A:
                        {
                            if (memberRefRemap.TryGetValue(oldToken, out var newMrToken))
                            {
                                tokenOperand.value = newMrToken;
                                remappedCount++;
                            }
                            else
                            {
                                failedCount++;
                            }

                            break;
                        }
                    }
                }
            }

            if (remappedCount > 0 || failedCount > 0)
            {
            }
        }


        /// <summary>
        ///     鐨凟ntryPoint 浠ょ墝瑙ｆ瀽鍏ュ彛鐨凪ethodDefinitionHandle锛堟垨 MemberReferenceHandle锛夌殑
        ///     浠ょ墝楂樺瓧鑺傛爣璇嗚〃绫诲瀷鐨剎06 = MethodDef鐨剎0A = MemberRef鐨?
        /// </summary>
        private static MethodDefinitionHandle resolve_entry_point_handle(
            ClrModuleData module,
            List<ClrMethodDef> methods,
            IReadOnlyDictionary<uint, MemberReferenceHandle> memberRefTokenMap)
        {
            var entryPoint = module.clr_directory.entry_point;
            if (entryPoint == 0) return default;

            var tableType = entryPoint >> 24;
            var rowNumber = (int)(entryPoint & 0x00FFFFFF);

            if (tableType == 0x06 && rowNumber > 0 && rowNumber <= methods.Count)
                return MetadataTokens.MethodDefinitionHandle(rowNumber);

            return default;
        }
    }

    #endregion

    #region PE 鏋勫缓鐨?

    /// <summary>
    ///     PE 鏂囦欢鏋勫缓鍣紝缁勮瀹屾暣鐨?NET 绋嬪簭闆嗙殑
    /// </summary>
    private sealed class PeBuilder
    {
        private readonly List<byte[]> _method_signatures = [];
        private readonly ClrModuleData _module;
        private readonly Dictionary<byte[], uint> _signature_blob_index_map = new(ByteArrayComparer.instance);
        private List<uint> _method_rvas = [];
        private Dictionary<string, uint> _string_indices = [];

        public PeBuilder(ClrModuleData module)
        {
            _module = module;
        }

        public byte[] build()
        {
            var stringHeap = build_string_heap();
            var blobHeap = build_blob_heap();
            var guidHeap = build_guid_heap();
            var userStringHeap = build_user_string_heap();

            // `.text` 鐨勮櫄鎷熷湴鍧€蹇呴』涓?SectionAlignment 瀵归綈銆?
            // OptionalHeader 鐨?SectionAlignment 鍥哄畾涓?0x2000锛屽洜姝?RVA 涓嶈兘浣跨敤 0x200銆?
            var textSectionRva = 0x2000;
            var ilSectionBytes = build_il_section(textSectionRva);

            var tableStream = build_table_stream();
            var metadataBytes = build_metadata(stringHeap, blobHeap, guidHeap, userStringHeap, tableStream);

            var textSectionData = new ByteBufferWriter(0x2000);

            textSectionData.write(ilSectionBytes);
            var clrDirectoryOffset = align_up((uint)textSectionData.position, 4u);
            while ((uint)textSectionData.position < clrDirectoryOffset) textSectionData.write_u8(0);

            var metadataOffset = align_up(clrDirectoryOffset + ClrConstants.clr_directory_size, 4u);

            var directoryFlags = _module.clr_directory.flags != 0
                ? _module.clr_directory.flags
                : (uint)ClrDirectoryFlags.il_only;
            var entryPoint = _module.clr_directory.entry_point;
            write_clr_directory(
                ref textSectionData,
                (uint)(metadataOffset + textSectionRva),
                (uint)metadataBytes.Length,
                directoryFlags,
                entryPoint);

            textSectionData.write(metadataBytes);

            var textSectionSize = (uint)((textSectionData.position + 0x1FF) & ~0x1FF);
            var peHeaderSize = 0x200;

            var writer = new ByteBufferWriter(peHeaderSize + (int)textSectionSize);

            write_dos_header(ref writer);
            write_pe_header(ref writer, (uint)textSectionRva, textSectionSize);
            write_optional_header(ref writer, (uint)textSectionRva, textSectionSize,
                (uint)(clrDirectoryOffset + textSectionRva), ClrConstants.clr_directory_size);
            write_section_header(ref writer, (uint)textSectionRva, textSectionSize);

            while (writer.position < peHeaderSize) writer.write_u8(0);

            writer.write(textSectionData.to_array());

            while (writer.position < peHeaderSize + (int)textSectionSize) writer.write_u8(0);

            return writer.to_array();
        }

        private static uint align_up(uint value, uint alignment)
        {
            if (alignment == 0) return value;

            var remainder = value % alignment;
            return remainder == 0 ? value : value + (alignment - remainder);
        }

        private byte[] build_metadata(byte[] stringHeap, byte[] blobHeap, byte[] guidHeap, byte[] userStringHeap,
            byte[] tableStream)
        {
            var headerSize = 16;
            var versionString = Encoding.UTF8.GetBytes(_module.version ?? "v4.0.30319");
            var versionLength = (4 + versionString.Length + 3) & ~3;

            var streamCount = 5;
            var streamHeaderSize = 0;

            var streamNames = new[]
            {
                ClrConstants.table_stream_name, ClrConstants.strings_stream_name, ClrConstants.blob_stream_name,
                ClrConstants.guid_stream_name, ClrConstants.user_string_stream_name
            };
            var streamData = new[] { tableStream, stringHeap, blobHeap, guidHeap, userStringHeap };

            for (var i = 0; i < streamCount; i++)
            {
                var nameBytes = Encoding.UTF8.GetBytes(streamNames[i] + "\0");
                var namePadded = (nameBytes.Length + 3) & ~3;
                streamHeaderSize += 8 + namePadded;
            }

            var streamOffsets = new int[streamCount];
            var currentOffset = headerSize + versionLength + 4 + streamHeaderSize;

            for (var i = 0; i < streamCount; i++)
            {
                streamOffsets[i] = currentOffset;
                currentOffset += streamData[i].Length;
            }

            var totalSize = headerSize + versionLength + 4 + streamHeaderSize;

            foreach (var sd in streamData) totalSize += sd.Length;

            var writer = new ByteBufferWriter(totalSize);

            writer.write_u32_le(ClrConstants.metadata_signature);
            writer.write_u16_le(1);
            writer.write_u16_le(1);
            writer.write_u32_le(0);
            writer.write_u32_le((uint)versionLength);
            writer.write(versionString);

            var versionPad = versionLength - versionString.Length;

            for (var i = 0; i < versionPad; i++) writer.write_u8(0);

            writer.write_u16_le(0);
            writer.write_u16_le((ushort)streamCount);

            for (var i = 0; i < streamCount; i++)
            {
                writer.write_u32_le((uint)streamOffsets[i]);
                writer.write_u32_le((uint)streamData[i].Length);
                var nameBytes = Encoding.UTF8.GetBytes(streamNames[i] + "\0");
                writer.write(nameBytes);
                var namePad = ((nameBytes.Length + 3) & ~3) - nameBytes.Length;

                for (var p = 0; p < namePad; p++) writer.write_u8(0);
            }

            foreach (var sd in streamData) writer.write(sd);

            return writer.to_array();
        }

        private byte[] build_string_heap()
        {
            var strings = new List<string>();
            _string_indices = new Dictionary<string, uint>
            {
                [string.Empty] = 0
            };

            void AddString(string s)
            {
                if (string.IsNullOrEmpty(s) || !_string_indices.TryAdd(s, 0)) return;

                strings.Add(s);
            }

            AddString(_module.module_name);
            AddString(get_assembly_simple_name());

            foreach (var type in _module.types)
            {
                AddString(type.name);
                AddString(type.@namespace);

                foreach (var field in type.fields) AddString(field.name);

                foreach (var method in type.methods) AddString(method.name);
            }

            foreach (var method in _module.methods) AddString(method.name);

            foreach (var field in _module.fields) AddString(field.name);

            var writer = new ByteBufferWriter(1 + strings.Sum(s => Encoding.UTF8.GetByteCount(s) + 1));
            writer.write_u8(0);

            foreach (var s in strings)
            {
                _string_indices[s] = (uint)writer.position;
                writer.write(Encoding.UTF8.GetBytes(s));
                writer.write_u8(0);
            }

            return writer.to_array();
        }

        private uint get_string_index(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;

            return _string_indices.TryGetValue(s, out var idx) ? idx : 0;
        }

        private byte[] build_blob_heap()
        {
            _method_signatures.Clear();
            _method_signatures.AddRange(_module.methods.Select(m =>
                m.signature.Length == 0 ? [0x00, 0x00, 0x01] : m.signature));

            foreach (var type in _module.types)
                _method_signatures.AddRange(type.methods.Select(m =>
                    m.signature.Length == 0 ? [0x00, 0x00, 0x01] : m.signature));

            var writer = new ByteBufferWriter(256);
            writer.write_u8(0);
            _signature_blob_index_map.Clear();

            foreach (var signature in _method_signatures)
            {
                if (_signature_blob_index_map.ContainsKey(signature)) continue;

                var index = (uint)writer.position;
                write_compressed_unsigned(ref writer, (uint)signature.Length);
                writer.write(signature);
                _signature_blob_index_map[signature] = index;
            }

            return writer.to_array();
        }

        private byte[] build_guid_heap()
        {
            var guid = _module.metadata.guid_heap.data;

            if (guid.Length > 0) return guid;

            return Guid.NewGuid().ToByteArray();
        }

        private byte[] build_user_string_heap()
        {
            return [0];
        }

        private byte[] build_table_stream()
        {
            var allMethods = new List<ClrMethodDef>();
            allMethods.AddRange(_module.methods);

            foreach (var type in _module.types) allMethods.AddRange(type.methods);

            var allFields = new List<ClrFieldDef>();
            allFields.AddRange(_module.types.SelectMany(t => t.fields));

            var moduleRowCount = 1u;
            var typeDefRowCount = (uint)_module.types.Count;
            var methodDefRowCount = (uint)allMethods.Count;
            var fieldRowCount = (uint)allFields.Count;
            var paramRowCount = 0u;
            var assemblyRowCount = 1u;

            var validTables = 0UL;
            validTables |= 1UL << (int)ClrTableKind.module;
            validTables |= 1UL << (int)ClrTableKind.type_def;
            validTables |= 1UL << (int)ClrTableKind.assembly;

            if (fieldRowCount > 0) validTables |= 1UL << (int)ClrTableKind.field;

            if (methodDefRowCount > 0)
            {
                validTables |= 1UL << (int)ClrTableKind.method_def;
                validTables |= 1UL << (int)ClrTableKind.param;
            }

            var heapSizes = (byte)0;
            var stringIndexSize = 2;
            var guidIndexSize = 2;
            var blobIndexSize = 2;

            var rowCounts = new List<uint>();

            if ((validTables & (1UL << (int)ClrTableKind.module)) != 0) rowCounts.Add(moduleRowCount);

            if ((validTables & (1UL << (int)ClrTableKind.type_def)) != 0) rowCounts.Add(typeDefRowCount);

            if ((validTables & (1UL << (int)ClrTableKind.field)) != 0) rowCounts.Add(fieldRowCount);

            if ((validTables & (1UL << (int)ClrTableKind.method_def)) != 0) rowCounts.Add(methodDefRowCount);

            if ((validTables & (1UL << (int)ClrTableKind.param)) != 0) rowCounts.Add(paramRowCount);

            if ((validTables & (1UL << (int)ClrTableKind.assembly)) != 0) rowCounts.Add(assemblyRowCount);

            var typeDefOrRefIndexSize = 2;
            var fieldTableIndexSize = fieldRowCount <= 0xFFFF ? 2 : 4;
            var methodDefTableIndexSize = methodDefRowCount <= 0xFFFF ? 2 : 4;
            var paramTableIndexSize = 2;

            var moduleRowSize = 2 + stringIndexSize + guidIndexSize * 3;
            var typeDefRowSize = 4 + stringIndexSize * 2 + typeDefOrRefIndexSize + fieldTableIndexSize +
                                 methodDefTableIndexSize;
            var fieldRowSize = 2 + stringIndexSize + blobIndexSize;
            var methodDefRowSize = 4 + 2 + 2 + stringIndexSize + blobIndexSize + paramTableIndexSize;
            var assemblyRowSize = 4 + 2 + 2 + 2 + 2 + 4 + blobIndexSize + stringIndexSize + stringIndexSize;

            var totalRowSize = moduleRowCount * (uint)moduleRowSize
                               + typeDefRowCount * (uint)typeDefRowSize
                               + fieldRowCount * (uint)fieldRowSize
                               + methodDefRowCount * (uint)methodDefRowSize
                               + assemblyRowCount * (uint)assemblyRowSize;

            var headerSize = 24 + rowCounts.Count * 4;
            var writer = new ByteBufferWriter(headerSize + (int)totalRowSize);

            writer.write_u32_le(0);
            writer.write_u8(2);
            writer.write_u8(0);
            writer.write_u8(heapSizes);
            writer.write_u8(0);
            writer.write_u64_le(validTables);
            // Sorted bitmask 浠呭湪纭疄婊¤冻瀵瑰簲琛ㄦ帓搴忕害鏉熸椂璁剧疆锛涜繖閲屼繚瀹堝啓 0锛岄伩鍏嶅０鏄庨敊璇帓搴忕殑            writer.WriteU64LE(0);

            foreach (var rc in rowCounts) writer.write_u32_le(rc);

            writer.write_u16_le(0);
            write_index(ref writer, get_string_index(_module.module_name), stringIndexSize);
            write_index(ref writer, 1, guidIndexSize);
            write_index(ref writer, 0, guidIndexSize);
            write_index(ref writer, 0, guidIndexSize);

            var methodIndex = 1 + _module.methods.Count;
            var fieldIndex = 1;

            foreach (var type in _module.types)
            {
                writer.write_u32_le((uint)type.flags);
                write_index(ref writer, get_string_index(type.name), stringIndexSize);
                write_index(ref writer, get_string_index(type.@namespace), stringIndexSize);
                write_index(ref writer, 0, typeDefOrRefIndexSize);
                write_index(ref writer, (uint)fieldIndex, fieldTableIndexSize);
                write_index(ref writer, (uint)methodIndex, methodDefTableIndexSize);

                fieldIndex += type.fields.Count;
                methodIndex += type.methods.Count;
            }

            foreach (var type in _module.types)
            foreach (var field in type.fields)
            {
                writer.write_u16_le((ushort)field.flags);
                write_index(ref writer, get_string_index(field.name), stringIndexSize);
                write_index(ref writer, 0, blobIndexSize);
            }

            var methodIdx = 0;

            foreach (var method in _module.methods)
            {
                var rva = methodIdx < _method_rvas.Count ? _method_rvas[methodIdx] : 0u;
                methodIdx++;
                writer.write_u32_le(rva);
                writer.write_u16_le(0);
                writer.write_u16_le((ushort)method.flags);
                write_index(ref writer, get_string_index(method.name), stringIndexSize);
                write_index(ref writer, get_method_signature_index(method), blobIndexSize);
                write_index(ref writer, 1, paramTableIndexSize);
            }

            foreach (var type in _module.types)
            foreach (var method in type.methods)
            {
                var rva = methodIdx < _method_rvas.Count ? _method_rvas[methodIdx] : 0u;
                methodIdx++;
                writer.write_u32_le(rva);
                writer.write_u16_le(0);
                writer.write_u16_le((ushort)method.flags);
                write_index(ref writer, get_string_index(method.name), stringIndexSize);
                write_index(ref writer, get_method_signature_index(method), blobIndexSize);
                write_index(ref writer, 1, paramTableIndexSize);
            }

            var (majorVersion, minorVersion, buildNumber, revisionNumber) = parse_version(_module.version);
            writer.write_u32_le(0x00008004);
            writer.write_u16_le(majorVersion);
            writer.write_u16_le(minorVersion);
            writer.write_u16_le(buildNumber);
            writer.write_u16_le(revisionNumber);
            writer.write_u32_le(0);
            write_index(ref writer, 0, blobIndexSize);
            write_index(ref writer, get_string_index(get_assembly_simple_name()), stringIndexSize);
            write_index(ref writer, 0, stringIndexSize);

            return writer.to_array();
        }

        private string get_assembly_simple_name()
        {
            if (string.IsNullOrWhiteSpace(_module.module_name)) return "Module";

            return Path.GetFileNameWithoutExtension(_module.module_name);
        }

        private static (ushort Major, ushort Minor, ushort Build, ushort Revision) parse_version(string? version)
        {
            if (Version.TryParse(version, out var parsed))
                return (
                    (ushort)System.Math.Clamp(parsed.Major, 0, ushort.MaxValue),
                    (ushort)System.Math.Clamp(parsed.Minor, 0, ushort.MaxValue),
                    (ushort)System.Math.Clamp(parsed.Build < 0 ? 0 : parsed.Build, 0, ushort.MaxValue),
                    (ushort)System.Math.Clamp(parsed.Revision < 0 ? 0 : parsed.Revision, 0, ushort.MaxValue)
                );

            return (1, 0, 0, 0);
        }

        private uint get_method_signature_index(ClrMethodDef method)
        {
            var signature = method.signature.Length == 0 ? [0x00, 0x00, 0x01] : method.signature;
            if (_signature_blob_index_map.TryGetValue(signature, out var index)) return index;

            return 0;
        }

        private static void write_compressed_unsigned(ref ByteBufferWriter writer, uint value)
        {
            if (value <= 0x7F)
            {
                writer.write_u8((byte)value);
                return;
            }

            if (value <= 0x3FFF)
            {
                writer.write_u8((byte)((value >> 8) | 0x80));
                writer.write_u8((byte)(value & 0xFF));
                return;
            }

            writer.write_u8((byte)((value >> 24) | 0xC0));
            writer.write_u8((byte)((value >> 16) & 0xFF));
            writer.write_u8((byte)((value >> 8) & 0xFF));
            writer.write_u8((byte)(value & 0xFF));
        }

        private static void write_index(ref ByteBufferWriter writer, uint value, int size)
        {
            if (size == 2)
                writer.write_u16_le((ushort)value);
            else
                writer.write_u32_le(value);
        }

        private byte[] build_il_section(int textSectionRva)
        {
            var writer = new ByteBufferWriter(0x80);
            _method_rvas = [];

            var allMethods = new List<ClrMethodDef>();
            allMethods.AddRange(_module.methods);

            foreach (var type in _module.types) allMethods.AddRange(type.methods);

            foreach (var method in allMethods)
            {
                if (method.instructions.Count == 0)
                {
                    _method_rvas.Add(0);
                    continue;
                }

                var rva = (uint)(writer.position + textSectionRva);
                _method_rvas.Add(rva);

                var bodyBytes = encode_method_body(method.instructions, method.max_stack, method.local_var_sig_tok,
                    method.exception_handlers, method.init_locals);
                writer.write(bodyBytes);

                var pad = (4 - bodyBytes.Length % 4) % 4;

                for (var i = 0; i < pad; i++) writer.write_u8(0);
            }

            return writer.to_array();
        }

        private static void write_dos_header(ref ByteBufferWriter writer)
        {
            writer.write_u16_le(0x5A4D);
            writer.write_u16_le(0x0090);
            writer.write_u16_le(0x0003);
            writer.write_u16_le(0x0000);
            writer.write_u16_le(0x0004);
            writer.write_u16_le(0x0000);
            writer.write_u16_le(0xFFFF);
            writer.write_u16_le(0x0000);
            writer.write_u16_le(0x00B8);
            writer.write_u16_le(0x0000);
            writer.write_u16_le(0x0000);
            writer.write_u16_le(0x0000);
            writer.write_u32_le(0x00000000);
            writer.write_u32_le(0x00000000);

            for (var i = 0; i < 14; i++) writer.write_u16_le(0x0000);

            writer.write_u32_le(0x00000040);
        }

        private static void write_pe_header(ref ByteBufferWriter writer, uint textSectionRva, uint textSectionSize)
        {
            writer.write_u32_le(0x00004550);
            writer.write_u16_le(0x014C);
            writer.write_u16_le(1);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u16_le(0x00E0);
            writer.write_u16_le(0x0022);
        }

        private static void write_optional_header(ref ByteBufferWriter writer, uint textSectionRva,
            uint textSectionSize,
            uint clrRva, uint clrSize)
        {
            writer.write_u16_le(0x010B);
            writer.write_u8(8);
            writer.write_u8(0);
            writer.write_u32_le(textSectionSize);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            // ILOnly 绋嬪簭闆嗙敱 CLR Header 鐨凟ntryPoint token 鍐冲畾鍏ュ彛锛屼笉鍐欏師鐨凴VA 鍏ュ彛鐨?           writer.WriteU32LE(0);
            writer.write_u32_le(textSectionRva);
            writer.write_u32_le(textSectionRva);
            writer.write_u32_le(0x00400000);
            writer.write_u32_le(0x2000);
            writer.write_u32_le(0x200);
            writer.write_u16_le(4);
            writer.write_u16_le(0);
            writer.write_u16_le(0);
            writer.write_u16_le(0);
            writer.write_u16_le(4);
            writer.write_u16_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(textSectionRva + textSectionSize);
            writer.write_u32_le(0x200);
            writer.write_u32_le(0);
            writer.write_u16_le(3);
            writer.write_u16_le(0x8540);
            writer.write_u32_le(0x00100000);
            writer.write_u32_le(0x1000);
            writer.write_u32_le(0x00100000);
            writer.write_u32_le(0x1000);
            writer.write_u32_le(0);
            writer.write_u32_le(16);

            for (var i = 0; i < 14; i++)
            {
                writer.write_u32_le(0);
                writer.write_u32_le(0);
            }

            writer.write_u32_le(clrRva);
            writer.write_u32_le(clrSize);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
        }

        private static void write_clr_directory(
            ref ByteBufferWriter writer,
            uint metadataRva,
            uint metadataSize,
            uint flags,
            uint entryPoint)
        {
            writer.write_u32_le(ClrConstants.clr_directory_size);
            writer.write_u16_le(2);
            writer.write_u16_le(5);
            writer.write_u32_le(metadataRva);
            writer.write_u32_le(metadataSize);
            writer.write_u32_le(flags);
            writer.write_u32_le(entryPoint);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
        }

        private static void write_section_header(ref ByteBufferWriter writer, uint rva, uint rawSize)
        {
            var name = Encoding.UTF8.GetBytes(".text\0\0\0");
            writer.write(name);
            writer.write_u32_le(rawSize);
            writer.write_u32_le(rva);
            writer.write_u32_le(rawSize);
            writer.write_u32_le(0x200);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u16_le(0);
            writer.write_u16_le(0);
            writer.write_u32_le(0x60000020);
        }

        private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public static readonly ByteArrayComparer instance = new();

            public bool Equals(byte[]? x, byte[]? y)
            {
                if (ReferenceEquals(x, y)) return true;

                if (x is null || y is null || x.Length != y.Length) return false;

                return x.AsSpan().SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                var hash = new HashCode();
                foreach (var b in obj) hash.Add(b);

                return hash.ToHashCode();
            }
        }
    }

    #endregion

    #region MSIL 鎸囦护缂栫爜

    private static int estimate_instruction_size(IReadOnlyList<ClrInstruction> instructions)
    {
        var size = 0;

        foreach (var instr in instructions)
            size += get_opcode_size(instr.opcode) + get_operand_size(instr.opcode, instr.operand);

        return size;
    }

    private static int get_opcode_size(ClrOpcode opcode)
    {
        return (ushort)opcode >= ClrConstants.two_byte_opcode_base ? 2 : 1;
    }

    private static int get_operand_size(ClrOpcode opcode, ClrOperand? operand)
    {
        return opcode switch
        {
            ClrOpcode.ldarg_s or ClrOpcode.ldarga_s or ClrOpcode.starg_s or ClrOpcode.ldloc_s or ClrOpcode.ldloca_s
                or ClrOpcode.stloc_s or ClrOpcode.ldc_i4_s or ClrOpcode.unaligned => 1,
            ClrOpcode.ldarg or ClrOpcode.ldarga or ClrOpcode.starg or ClrOpcode.ldloc or ClrOpcode.ldloca
                or ClrOpcode.stloc => 2,
            ClrOpcode.ldc_i4 => 4,
            ClrOpcode.ldc_i8 => 8,
            ClrOpcode.ldc_r4 => 4,
            ClrOpcode.ldc_r8 => 8,
            ClrOpcode.br_s or ClrOpcode.brfalse_s or ClrOpcode.brtrue_s or ClrOpcode.beq_s or ClrOpcode.bne_un_s
                or ClrOpcode.blt_s or ClrOpcode.ble_s or ClrOpcode.bgt_s or ClrOpcode.bge_s or ClrOpcode.blt_un_s
                or ClrOpcode.ble_un_s or ClrOpcode.bgt_un_s or ClrOpcode.bge_un_s or ClrOpcode.leave_s => 1,
            ClrOpcode.br or ClrOpcode.brfalse or ClrOpcode.brtrue or ClrOpcode.beq or ClrOpcode.bne_un or ClrOpcode.blt
                or ClrOpcode.ble or ClrOpcode.bgt or ClrOpcode.bge or ClrOpcode.blt_un or ClrOpcode.ble_un
                or ClrOpcode.bgt_un or ClrOpcode.bge_un or ClrOpcode.leave => 4,
            ClrOpcode.@switch => operand is ClrSwitchTargetsOperand sw ? 4 + sw.offsets.Count * 4 : 4,
            ClrOpcode.call or ClrOpcode.callvirt or ClrOpcode.newobj or ClrOpcode.ldstr or ClrOpcode.ldftn
                or ClrOpcode.ldvirtftn or ClrOpcode.castclass or ClrOpcode.isinst or ClrOpcode.unbox
                or ClrOpcode.unbox_any or ClrOpcode.box or ClrOpcode.newarr or ClrOpcode.ldelema or ClrOpcode.initobj
                or ClrOpcode.constrained or ClrOpcode.jmp or ClrOpcode.calli or ClrOpcode.ldobj or ClrOpcode.stobj
                or ClrOpcode.ldfld or ClrOpcode.ldflda or ClrOpcode.stfld or ClrOpcode.ldsfld or ClrOpcode.ldsflda
                or ClrOpcode.stsfld or ClrOpcode.@sizeof or ClrOpcode.ldelem_any or ClrOpcode.stelem_any
                or ClrOpcode.cpobj or ClrOpcode.mkrefany or ClrOpcode.refanyval or ClrOpcode.ldtoken => 4,
            _ => 0
        };
    }

    private static void write_instruction(ref ByteBufferWriter writer, ClrInstruction instr)
    {
        write_opcode(ref writer, instr.opcode);
        write_operand(ref writer, instr.opcode, instr.operand);
    }

    private static void write_opcode(ref ByteBufferWriter writer, ClrOpcode opcode)
    {
        var value = (ushort)opcode;

        if (value >= ClrConstants.two_byte_opcode_base)
        {
            writer.write_u8(ClrConstants.two_byte_opcode_prefix);
            writer.write_u8((byte)(value & 0xFF));
        }
        else
        {
            writer.write_u8((byte)value);
        }
    }

    private static void write_operand(ref ByteBufferWriter writer, ClrOpcode opcode, ClrOperand? operand)
    {
        switch (opcode)
        {
            case ClrOpcode.ldarg_s:
            case ClrOpcode.ldarga_s:
            case ClrOpcode.starg_s:
                writer.write_u8((byte)((ClrArgumentIndexOperand?)operand!).index);
                break;
            case ClrOpcode.ldloc_s:
            case ClrOpcode.ldloca_s:
            case ClrOpcode.stloc_s:
                writer.write_u8((byte)((ClrLocalIndexOperand?)operand!).index);
                break;
            case ClrOpcode.ldarg:
            case ClrOpcode.ldarga:
            case ClrOpcode.starg:
                writer.write_u16_le((ushort)((ClrArgumentIndexOperand?)operand!).index);
                break;
            case ClrOpcode.ldloc:
            case ClrOpcode.ldloca:
            case ClrOpcode.stloc:
                writer.write_u16_le((ushort)((ClrLocalIndexOperand?)operand!).index);
                break;
            case ClrOpcode.ldc_i4_s:
                writer.write_i8(((ClrInt8Operand?)operand!).value);
                break;
            case ClrOpcode.ldc_i4:
                writer.write_i32_le(((ClrInt32Operand?)operand!).value);
                break;
            case ClrOpcode.ldc_i8:
                writer.write_i64_le(((ClrInt64Operand?)operand!).value);
                break;
            case ClrOpcode.ldc_r4:
                writer.write_f32_le(((ClrFloat32Operand?)operand!).value);
                break;
            case ClrOpcode.ldc_r8:
                writer.write_f64_le(((ClrFloat64Operand?)operand!).value);
                break;
            case ClrOpcode.br_s:
            case ClrOpcode.brfalse_s:
            case ClrOpcode.brtrue_s:
            case ClrOpcode.beq_s:
            case ClrOpcode.bne_un_s:
            case ClrOpcode.blt_s:
            case ClrOpcode.ble_s:
            case ClrOpcode.bgt_s:
            case ClrOpcode.bge_s:
            case ClrOpcode.blt_un_s:
            case ClrOpcode.ble_un_s:
            case ClrOpcode.bgt_un_s:
            case ClrOpcode.bge_un_s:
            case ClrOpcode.leave_s:
            {
                var target = (ClrBranchTarget8Operand?)operand!;
                writer.write_i8((sbyte)(target.offset - (writer.position + 1)));
                break;
            }
            case ClrOpcode.br:
            case ClrOpcode.brfalse:
            case ClrOpcode.brtrue:
            case ClrOpcode.beq:
            case ClrOpcode.bne_un:
            case ClrOpcode.blt:
            case ClrOpcode.ble:
            case ClrOpcode.bgt:
            case ClrOpcode.bge:
            case ClrOpcode.blt_un:
            case ClrOpcode.ble_un:
            case ClrOpcode.bgt_un:
            case ClrOpcode.bge_un:
            case ClrOpcode.leave:
            {
                var target = (ClrBranchTarget32Operand?)operand!;
                writer.write_i32_le(target.offset - (writer.position + 4));
                break;
            }
            case ClrOpcode.@switch:
            {
                var sw = (ClrSwitchTargetsOperand?)operand!;
                writer.write_u32_le((uint)sw.offsets.Count);
                var baseOffset = writer.position + sw.offsets.Count * 4;

                foreach (var target in sw.offsets) writer.write_i32_le(target - baseOffset);

                break;
            }
            case ClrOpcode.call:
            case ClrOpcode.callvirt:
            case ClrOpcode.newobj:
            case ClrOpcode.ldstr:
            case ClrOpcode.ldftn:
            case ClrOpcode.ldvirtftn:
            case ClrOpcode.castclass:
            case ClrOpcode.isinst:
            case ClrOpcode.unbox:
            case ClrOpcode.unbox_any:
            case ClrOpcode.box:
            case ClrOpcode.newarr:
            case ClrOpcode.ldelema:
            case ClrOpcode.initobj:
            case ClrOpcode.constrained:
            case ClrOpcode.jmp:
            case ClrOpcode.calli:
            case ClrOpcode.ldobj:
            case ClrOpcode.stobj:
            case ClrOpcode.ldfld:
            case ClrOpcode.ldflda:
            case ClrOpcode.stfld:
            case ClrOpcode.ldsfld:
            case ClrOpcode.ldsflda:
            case ClrOpcode.stsfld:
            case ClrOpcode.@sizeof:
            case ClrOpcode.ldelem_any:
            case ClrOpcode.stelem_any:
            case ClrOpcode.cpobj:
            case ClrOpcode.mkrefany:
            case ClrOpcode.refanyval:
            case ClrOpcode.ldtoken:
                writer.write_u32_le(((ClrTokenOperand?)operand!).value);
                break;
            case ClrOpcode.unaligned:
                writer.write_u8((byte)((ClrInt8Operand?)operand!).value);
                break;
        }
    }

    #endregion
}
