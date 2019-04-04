// 检查生成的 DLL 中的类型定义，定位运行时错误
import { execSync } from 'child_process';

const dllPath = 'E:\\RiderProjects\\valkyrie.v\\projects\\legion.tools\\dist\\clr-microsoft-unknown-managed\\legion_tools.dll';

// 使用 dotnet 运行一个内联脚本来反射加载并检查类型
const code = `
using System;
using System.Reflection;
using System.Linq;

try
{
    var asm = Assembly.LoadFrom(@"${dllPath.replace(/\\/g, '\\\\')}");
    Console.WriteLine("=== 程序集加载成功 ===");
    Console.WriteLine($"名称: {asm.GetName().Name}");
    Console.WriteLine($"版本: {asm.GetName().Version}");
    Console.WriteLine();
    
    var types = asm.GetTypes();
    Console.WriteLine($"类型总数: {types.Length}");
    Console.WriteLine();
    
    Console.WriteLine("=== 所有类型 ===");
    foreach (var t in types.OrderBy(x => x.FullName))
    {
        var parent = t.BaseType != null ? $" : {t.BaseType.FullName}" : "";
        Console.WriteLine($"  {t.FullName}{parent}");
    }
}
catch (ReflectionTypeLoadException ex)
{
    Console.WriteLine("=== ReflectionTypeLoadException ===");
    foreach (var e in ex.LoaderExceptions)
    {
        if (e != null)
            Console.WriteLine($"  加载异常: {e.Message}");
    }
    Console.WriteLine();
    Console.WriteLine("=== 成功加载的类型 ===");
    foreach (var t in ex.Types.Where(x => x != null).OrderBy(x => x.FullName))
    {
        var parent = t.BaseType != null ? $" : {t.BaseType.FullName}" : "";
        Console.WriteLine($"  {t.FullName}{parent}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"错误: {ex.GetType().Name}: {ex.Message}");
}
`;

const tmpDir = 'E:\\RiderProjects\\tmp_inspect';
execSync(`mkdir "${tmpDir}" 2>nul & echo.`, { shell: 'powershell' });

const tmpProj = `${tmpDir}/inspect.csproj`;
const tmpCode = `${tmpDir}/Program.cs`;

const fs = await import('fs');
fs.writeFileSync(tmpProj, `<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>`);
fs.writeFileSync(tmpCode, code);

try {
    const result = execSync(`dotnet run --project "${tmpProj}"`, { 
        cwd: 'E:\\RiderProjects\\tmp_inspect',
        encoding: 'utf8',
        timeout: 60000 
    });
    console.log(result);
} catch (e) {
    console.log('stdout:', e.stdout);
    console.log('stderr:', e.stderr);
} finally {
    // 清理
    try { fs.rmSync(tmpDir, { recursive: true, force: true }); } catch {}
}