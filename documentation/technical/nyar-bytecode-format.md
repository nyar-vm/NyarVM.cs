# .nyar 瀛楄妭鐮佹ā鍧楁牸寮忚鑼?v1.0

## 姒傝堪

`.nyar` 鏂囦欢鏄?Nyar VM 鐨勫瓧鑺傜爜妯″潡鏍煎紡锛岀敱 Nyar 缂栬瘧鍣ㄥ伐鍏烽摼浜у嚭锛岀敱 Nyar VM 鍔犺浇鎵ц銆?
Nyar VM 鏄熀浜?Standard 鏂硅█鐨勯珮搴︾壒鍖栭€氱敤铏氭嫙鏈猴紝鍏跺瓧鑺傜爜鏍煎紡閽堝閫氱敤璁＄畻鍦烘櫙浼樺寲锛屾彁渚涘畬鏁寸殑绠楁湳銆佺被鍨嬭浆鎹€佸唴瀛樸€佸璞″拰瀛楃涓叉搷浣溿€?
## 璁捐鍘熷垯

- **楂樺害鐗瑰寲**锛氬瓧鑺傜爜鏍煎紡涓?Standard 鏂硅█閲忚韩瀹氬埗锛屼笉鍋氶€氱敤鍖栧Ε鍗?- **鍗曚竴鏍煎紡**锛氭墍鏈?`.nyar` 鏂囦欢浣跨敤缁熶竴鐨?NYAR 榄旀暟鍜屼簩杩涘埗甯冨眬锛屼笉瀛樺湪鍙樹綋
- **鏍堝紡鎵ц**锛氶噰鐢ㄦ爤寮忓瓧鑺傜爜妯″瀷锛岀被浼?WebAssembly 鐨勬搷浣滅爜浣撶郴
- **鍒嗘甯冨眬**锛氶噰鐢ㄦ琛紙Section Table锛夌粍缁囷紝渚夸簬鎸夐渶鍔犺浇鍜屾墿灞?
## 涓?.gnosis 鐨勫叧绯?
Nyar VM 鍜?Gnosis VM 鏄?Nyar 鍏冪紪璇戝櫒妗嗘灦浜у嚭鐨勪袱涓钩绾ц櫄鎷熸満锛?
| VM | 鍩轰簬鏂硅█ | 瀛楄妭鐮佹牸寮?| 榄旀暟 | 瀹氫綅 |
|:---|:---|:---|:---|:---|
| **Nyar VM** | Standard 鏂硅█ | `.nyar` | `0x4E594152` ("NYAR") | 閫氱敤璁＄畻铏氭嫙鏈?|
| **Gnosis VM** | Game 鏂硅█ | `.gnosis` | `0x474E4F53` ("GNOS") | 娓告垙鍦烘櫙涓撶敤铏氭嫙鏈?|

涓よ€呭悇鑷壒鍖栵紝浜掍笉鍏煎銆傚闇€鍏朵粬鍦烘櫙鐨勮櫄鎷熸満锛屽簲瀹氫箟鏂扮殑鏂硅█鍜屽搴旂殑瀛楄妭鐮佹牸寮忥紝鑰岄潪澶嶇敤鐜版湁鏍煎紡銆?
## 鏂囦欢缁撴瀯

```
+----------------------+
| Magic (4 bytes)      | 0x4E 0x59 0x41 0x52 ("NYAR")
+----------------------+
| Version (4 bytes)    | u32 = 1
+----------------------+
| Section Count (4B)   | u32
+----------------------+
| Name Offset (4B)     | u32
+----------------------+
| Section Headers      | SectionHeader[count]
+----------------------+
| Name Section         | length(i32) + UTF-8 bytes
+----------------------+
| Section Data...      | 鍚勬鏁版嵁鎸夋澶村亸绉绘帓鍒?+----------------------+
```

## 瀛楁璇︾粏璇存槑

### 鏂囦欢澶达紙16 瀛楄妭锛?
| 鍋忕Щ | 澶у皬 | 瀛楁 | 鎻忚堪 |
|:---|:---|:---|:---|
| 0x00 | 4 | Magic | 榄旀暟 `0x4E594152`锛?NYAR"锛屽皬绔簭瀛樺偍锛?|
| 0x04 | 4 | Version | 鐗堟湰鍙凤紝褰撳墠涓?`1` |
| 0x08 | 4 | SectionCount | 娈垫暟閲?|
| 0x0C | 4 | NameOffset | 妯″潡鍚嶇О娈电殑鏂囦欢鍋忕Щ |

### 娈靛ご锛堟瘡涓?9 瀛楄妭锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| Kind | u8 | 娈电被鍨?|
| Offset | i32 | 娈垫暟鎹湪鏂囦欢涓殑鍋忕Щ |
| Size | i32 | 娈垫暟鎹ぇ灏?|

### 娈电被鍨?
| Kind | 鍚嶇О | 鎻忚堪 |
|:---|:---|:---|
| 0x01 | Constants | 甯搁噺姹?|
| 0x02 | Functions | 鍑芥暟琛?|
| 0x03 | Code | 浠ｇ爜娈?|
| 0x04 | Imports | 瀵煎叆琛?|
| 0x05 | Exports | 瀵煎嚭琛?|
| 0x10 | DebugInfo | 璋冭瘯淇℃伅 |
| 0x11 | SourceMap | 婧愮爜鏄犲皠 |

### 鍚嶇О娈?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| length | i32 | 妯″潡鍚嶇О UTF-8 瀛楄妭闀垮害 |
| name | byte[length] | UTF-8 缂栫爜鐨勬ā鍧楀悕绉?|

### 甯搁噺姹犳锛圞ind = 0x01锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| count | i32 | 甯搁噺鏉＄洰鏁伴噺 |
| entries | ConstantEntry[count] | 甯搁噺鏉＄洰搴忓垪 |

姣忎釜 ConstantEntry锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| kind | u8 | 甯搁噺绫诲瀷 |
| data | 鍙橀暱 | 甯搁噺鏁版嵁锛堢敱 kind 鍐冲畾鏍煎紡锛?|

甯搁噺绫诲瀷锛?
| kind | 绫诲瀷 | data 鏍煎紡 |
|:---|:---|:---|
| 0x01 | Int32 | value(i32) |
| 0x02 | Float64 | value(f64) |
| 0x03 | Bool | value(u8, 0 鎴?1) |
| 0x04 | Null | 鏃犻檮鍔犳暟鎹?|
| 0x05 | Utf8 | length(i32) + UTF-8 bytes |
| 0x06 | BigInt | length(i32) + bytes |

### 鍑芥暟琛ㄦ锛圞ind = 0x02锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| count | i32 | 鍑芥暟鏁伴噺 |
| entries | FunctionEntry[count] | 鍑芥暟鏉＄洰搴忓垪 |

姣忎釜 FunctionEntry锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| name | Utf8Entry | 鍑芥暟鍚嶇О |
| parameterCount | i32 | 鍙傛暟鏁伴噺 |
| localCount | i32 | 灞€閮ㄥ彉閲忔暟閲?|
| codeLength | i32 | 浠ｇ爜闀垮害 |

### 浠ｇ爜娈碉紙Kind = 0x03锛?
鍘熷瀛楄妭鐮佹暟鎹紝鍑芥暟閫氳繃鍑芥暟琛ㄤ腑鐨勫亸绉诲拰闀垮害寮曠敤浠ｇ爜娈典腑鐨勫尯鍩熴€?
### 瀵煎叆琛ㄦ锛圞ind = 0x04锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| count | i32 | 瀵煎叆鏉＄洰鏁伴噺 |
| entries | ImportEntry[count] | 瀵煎叆鏉＄洰搴忓垪 |

姣忎釜 ImportEntry锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| kind | u8 | 瀵煎叆绫诲瀷锛?=Function, 1=Global, 2=Module锛?|
| moduleName | Utf8Entry | 妯″潡鍚嶇О |
| symbolName | Utf8Entry | 绗﹀彿鍚嶇О |

### 瀵煎嚭琛ㄦ锛圞ind = 0x05锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| count | i32 | 瀵煎嚭鏉＄洰鏁伴噺 |
| entries | ExportEntry[count] | 瀵煎嚭鏉＄洰搴忓垪 |

姣忎釜 ExportEntry锛?
| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| kind | u8 | 瀵煎嚭绫诲瀷锛?=Function, 1=Global锛?|
| symbolName | Utf8Entry | 绗﹀彿鍚嶇О |

### Utf8Entry 缂栫爜

| 瀛楁 | 绫诲瀷 | 鎻忚堪 |
|:---|:---|:---|
| length | i32 | UTF-8 瀛楄妭闀垮害 |
| bytes | byte[length] | UTF-8 缂栫爜瀛楃涓?|

## 鎸囦护闆?
Nyar VM 鎸囦护闆嗕负 Standard 鏂硅█鐗瑰寲锛屾搷浣滅爜涓哄崟瀛楄妭銆?
### 鎸囦护闆嗗竷灞€

| 鑼冨洿 | 绫诲埆 |
|:---|:---|
| 0x00-0x0C | 鎺у埗娴侊紙Nop, Jump, JumpIfTrue/False, Call, Return, TailCall, Throw, Catch, Yield, Resume, EffectHandle锛?|
| 0x10-0x13 | 鏍堟搷浣滐紙Const, Pop, Dup, Swap锛?|
| 0x20-0x24 | 灞€閮ㄥ彉閲忥紙LoadLocal, StoreLocal, LoadArg, LoadGlobal, StoreGlobal锛?|
| 0x30-0x3E | i32 绠楁湳/浣嶈繍绠?|
| 0x40-0x49 | i32 姣旇緝 |
| 0x50-0x55 | i64 绠楁湳 |
| 0x60-0x64 | f32 绠楁湳 |
| 0x70-0x74 | f64 绠楁湳 |
| 0x80-0x85 | 绫诲瀷杞崲 |
| 0x90-0x95 | 鍐呭瓨鎿嶄綔锛圓lloc, Free, Load/Store锛?|
| 0xA0-0xA8 | 瀵硅薄鎿嶄綔锛圢ewObject, Get/SetField, Get/SetIndex, Length, Closure锛?|
| 0xB0-0xB3 | 瀛楃涓叉搷浣?|
| 0xC0-0xC2 | BigInt 鎿嶄綔 |
| 0xD0-0xDC | 鍐呯疆鍑芥暟锛圥rint, Println, Exit, GetTime, Sleep, Math锛?|
| 0xE0 | BuiltinCall锛堣繍琛屾椂鍔ㄦ€佸垎娲撅級 |

## 瀛楄妭搴?
鎵€鏈夊瀛楄妭鏁存暟閲囩敤**灏忕搴?*锛圠ittle-Endian锛夈€?
## 鍘嗗彶鍙樻洿

| 鐗堟湰 | 鏃ユ湡 | 鍙樻洿 |
|:---|:---|:---|
| v1.0 | 2026-04-25 | 鍒濆瑙勮寖銆傛槑纭?.nyar 涓?Standard 鏂硅█鐗瑰寲鏍煎紡锛岀Щ闄ゅ鏂硅█鎵╁睍鏈哄埗 |

## 鉀?宸插簾寮?
| 璁捐 | 鐘舵€?| 璇存槑 |
|:---|:---|:---|
| 澶氭柟瑷€ opcode锛堥珮 8 浣嶆柟瑷€ ID锛?| **宸插簾寮?* | .nyar 鏍煎紡涓?Standard 鏂硅█鐗瑰寲锛屼笉鏀寔澶氭柟瑷€娣风紪銆傞渶瑕佸叾浠栨柟瑷€鐨?VM 搴斿畾涔夌嫭绔嬬殑瀛楄妭鐮佹牸寮?|
| 灏忕搴忛瓟鏁?`0x6E796172`锛?nyar"锛?| **宸插簾寮?* | 缁熶竴浣跨敤澶х搴忓瓨鍌ㄧ殑 `0x4E594152`锛?NYAR"锛?|
| SSA 鎰忓浘鑺傜偣缂栫爜 | **宸插簾寮?* | 瀹為檯瀹炵幇涓烘爤寮忓瓧鑺傜爜锛岃鑼冧笌瀹炵幇瀵归綈 |
| 鐙珛瀛楃涓茶〃娈?| **宸插簾寮?* | 瀛楃涓插唴鑱斿湪鍚勬涓紝浣跨敤 length+bytes 缂栫爜 |
| 鐙珛绫诲瀷娈?| **宸插簾寮?* | 褰撳墠鐗堟湰涓嶅寘鍚嫭绔嬬被鍨嬩俊鎭 |

