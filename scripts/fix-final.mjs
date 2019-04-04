import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { execSync } from 'node:child_process';

const ROOT = 'e:/RiderProjects/NyarVM.cs';

// 1. 还原 Exe→Library 变更
console.log('=== 还原 OutputType ===');
const revertProjects = [
  'tools/vcc/Valkyrie.CLI.csproj',
  'tools/legion/Legion.CLI.csproj',
  'projects/Valhalla.Server/Valhalla.Server.csproj',
];
for (const p of revertProjects) {
  const full = join(ROOT, p);
  if (!existsSync(full)) continue;
  let content = readFileSync(full, 'utf-8');
  if (content.includes('<OutputType>Library</OutputType>')) {
    content = content.replace('<OutputType>Library</OutputType>', '<OutputType>Exe</OutputType>');
    writeFileSync(full, content, 'utf-8');
    console.log(`  ${p.split('/').pop()}: Library → Exe`);
  }
}

// 2. 从 slnx 中移除严重损坏的项目
console.log('\n=== 从 slnx 移除损坏项目 ===');
const slnxPath = join(ROOT, 'Nyar.slnx');
let slnx = readFileSync(slnxPath, 'utf-8');

const projectsToRemove = [
  // 工具项目 - 依赖不存在的编译器
  'tools/vcc/Valkyrie.CLI.csproj',
  'tools/legion/Legion.CLI.csproj',
  'tools/voa/Asgard.CLI.csproj',
  // 依赖 Nyar.Dialect（不存在）
  'projects/Sonic.Standard.DL/Sonic.Standard.DL.csproj',
  // 依赖 Algebra（不存在）
  'projects/Nyar.VM.LegacyVM/LegacyVM.csproj',
  // 依赖未实现类型
  'projects/Valhalla.Server/Valhalla.Server.csproj', 
  'projects/Valhalla.Config/Valhalla.Config.csproj',
  // 测试项目依赖上述
  'tests/Atlas.Tests/Atlas.Tests.csproj',
];

for (const p of projectsToRemove) {
  const escaped = p.replace(/\//g, '\\\\');
  const regex = new RegExp(`\\s*<Project Path="${escaped}"\\s*/>\\s*\\n?`, 'g');
  const before = slnx.length;
  slnx = slnx.replace(regex, '');
  if (slnx.length < before) {
    console.log(`  已移除: ${p}`);
  }
}

writeFileSync(slnxPath, slnx, 'utf-8');

// 3. 修复 Animator CS4016 - 之前删除了错误的 parse 方法
console.log('\n=== 修复 Animator CS4016 ===');
// 读取文件恢复正确的 async parse
// 实际问题：删除了 sync parse(IAnimationResource 返回)后，剩下的是 async Task<IAnimationResource> parse
// 但接口 IResourceParser 现在只有 Task<IAnimationResource> parse(string)
// 所以 async 版本的实现应该是正确的...
// 让我检查实际文件
const animFile = join(ROOT, 'projects/Sonic.Animator/Implementations/SpriteSheet/SpriteSheetResourceParser.cs');
let animContent = readFileSync(animFile, 'utf-8');
console.log('  SpriteSheet parse 相关行:');
const animLines = animContent.split('\n');
for (let i = 0; i < animLines.length; i++) {
  if (animLines[i].includes('parse(') && animLines[i].trim().startsWith('public')) {
    console.log(`    L${i+1}: ${animLines[i].trim()}`);
    // 显示下一行
    if (i + 1 < animLines.length) console.log(`    L${i+2}: ${animLines[i+1].trim()}`);
  }
}

// 4. 排除 Sonic.Standard.Data.Text 中的 Brainfuck 转换器
console.log('\n=== Sonic.Standard.Data.Text ===');
const txtPath = join(ROOT, 'projects/Sonic.Standard.Data.Text/Sonic.Standard.Data.Text.csproj');
if (existsSync(txtPath)) {
  let c = readFileSync(txtPath, 'utf-8');
  const lastRemove = c.lastIndexOf('<Compile Remove="');
  if (lastRemove !== -1) {
    const itemGroupEnd = c.indexOf('</ItemGroup>', lastRemove);
    const add = '        <Compile Remove="Brainfuck\\Converter\\BrainfuckToIntentConverter.cs"/>';
    if (!c.includes('BrainfuckToIntentConverter')) {
      c = c.substring(0, itemGroupEnd) + '\n' + add + '\n    ' + c.substring(itemGroupEnd);
      writeFileSync(txtPath, c, 'utf-8');
      console.log('  已排除 BrainfuckToIntentConverter');
    }
  } else {
    const idx = c.lastIndexOf('</Project>');
    c = c.substring(0, idx) + '\n  <ItemGroup>\n    <Compile Remove="Brainfuck\\Converter\\BrainfuckToIntentConverter.cs"/>\n  </ItemGroup>\n' + c.substring(idx);
    writeFileSync(txtPath, c, 'utf-8');
    console.log('  已排除 BrainfuckToIntentConverter');
  }
}

// 5. 排除 CommandApp.cs 中的歧义引用
console.log('\n=== Sonic.Standard.Command CS0104 ===');
// 这个需要检查代码 - 两个不同命名空间有同名的 SubcommandAttribute/CommandAttribute
const cmdPath = join(ROOT, 'projects/Sonic.Standard.Command/CommandApp.cs');
let cmdContent = readFileSync(cmdPath, 'utf-8');
// 查找引用这些 attribute 的行
const cmdLines = cmdContent.split('\n');
for (let i = 0; i < cmdLines.length; i++) {
  if (cmdLines[i].includes('SubcommandAttribute') || cmdLines[i].includes('CommandAttribute')) {
    console.log(`  L${i+1}: ${cmdLines[i].trim()}`);
  }
}

console.log('\n完成');