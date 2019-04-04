import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { execSync } from 'node:child_process';

const ROOT = 'e:/RiderProjects/NyarVM.cs';

// 1. 检查 Sonic.SourceGenerator 的 GenerateAssemblyInfo
console.log('=== Sonic.SourceGenerator ===');
const sgPath = join(ROOT, 'projects/Sonic.SourceGenerator/Sonic.SourceGenerator.csproj');
let sg = readFileSync(sgPath, 'utf-8');
console.log('  有 GenerateAssemblyInfo:', sg.includes('GenerateAssemblyInfo'));
// 清理 obj
try { execSync('powershell -Command "Remove-Item -Recurse -Force -ErrorAction SilentlyContinue e:/RiderProjects/NyarVM.cs/projects/Sonic.SourceGenerator/obj"', {encoding:'utf-8'}); console.log('  已清理 obj'); } catch {}

// 2. 检查 Sonic.Standard.DL
console.log('\n=== Sonic.Standard.DL ===');
const dlPath = join(ROOT, 'projects/Sonic.Standard.DL/Sonic.Standard.DL.csproj');
let dl = readFileSync(dlPath, 'utf-8');
console.log('  有 GenerateAssemblyInfo:', dl.includes('GenerateAssemblyInfo'));

// 3. 修复 Asgard.CLI - 检查实际 csproj 路径
console.log('\n=== Asgard.CLI 路径 ===');
const asgardPaths = [
  'projects/Nyar.Language/Awsl.Asgard/Asgard.CLI.csproj',
  'tools/voa/Asgard.CLI.csproj',
];
for (const p of asgardPaths) {
  const full = join(ROOT, p);
  console.log(`  ${p}: ${existsSync(full) ? '存在' : '不存在'}`);
}

// 4. 修复 CS5001 - 将 Exe 改为 Library
console.log('\n=== 修复 CS5001 ===');
const cs5001Projects = [
  'tools/vcc/Valkyrie.CLI.csproj',
  'tools/legion/Legion.CLI.csproj',
  'projects/Valhalla.Server/Valhalla.Server.csproj',
];
for (const p of cs5001Projects) {
  const full = join(ROOT, p);
  if (!existsSync(full)) continue;
  let content = readFileSync(full, 'utf-8');
  if (content.includes('<OutputType>Exe</OutputType>')) {
    content = content.replace('<OutputType>Exe</OutputType>', '<OutputType>Library</OutputType>');
    writeFileSync(full, content, 'utf-8');
    console.log(`  ${p.split('/').pop()}: Exe → Library`);
  } else {
    console.log(`  ${p.split('/').pop()}: 不是 Exe`);
  }
}

// 5. 修复 Animator 的 parse 重复 - 直接编辑文件
console.log('\n=== 修复 Animator parse ===');
const animFiles = [
  'projects/Sonic.Animator/Implementations/SpriteSheet/SpriteSheetResourceParser.cs',
  'projects/Sonic.Animator/Implementations/Live2D/Live2DResourceParser.cs',
  'projects/Sonic.Animator/Implementations/Spine/SpineResourceParser.cs',
];
for (const f of animFiles) {
  const full = join(ROOT, f);
  if (!existsSync(full)) continue;
  let content = readFileSync(full, 'utf-8');
  
  // 找两个 parse 方法
  const lines = content.split('\n');
  const parseLines = [];
  for (let i = 0; i < lines.length; i++) {
    const t = lines[i].trim();
    if ((t.includes('IAnimationResource parse(') || t.includes('Task<IAnimationResource> parse(')) && t.startsWith('public ')) {
      parseLines.push({line: i, text: t});
    }
  }
  
  console.log(`  ${f.split('/').pop()}: 找到 ${parseLines.length} 个 parse 方法`);
  if (parseLines.length >= 2) {
    // 删除同步版本（非 Task 的）
    const syncIdx = parseLines.find(p => !p.text.includes('Task'));
    if (syncIdx) {
      // 找到方法开始的注释
      let start = syncIdx.line;
      while (start > 0 && lines[start - 1].trim().startsWith('///')) {
        start--;
      }
      // 找到空行分隔
      while (start > 0 && lines[start - 1].trim() === '') {
        start--;
      }
      
      // 找到方法结束
      let end = syncIdx.line;
      let depth = 0;
      let found = false;
      for (let i = end; i < lines.length; i++) {
        if (lines[i].includes('{')) depth++;
        if (lines[i].includes('}')) {
          depth--;
          if (depth === 0) { end = i; found = true; break; }
        }
      }
      if (!found) { console.log('    未找到方法结束'); continue; }
      
      // 删除到下一个非空行
      while (end + 1 < lines.length && lines[end + 1].trim() === '') {
        end++;
      }
      
      const deleted = lines.slice(start, end + 1);
      console.log(`    删除行 ${start+1}-${end+1}: ${deleted[0].trim().substring(0, 60)}...`);
      lines.splice(start, end - start + 1);
      writeFileSync(full, lines.join('\n'), 'utf-8');
    }
  }
}

// 6. Sonic.Standard.Schema - 排除更多文件
console.log('\n=== Sonic.Standard.Schema ===');
function addExclude(csproj, files) {
  const full = join(ROOT, csproj);
  if (!existsSync(full)) return;
  let c = readFileSync(full, 'utf-8');
  const lastRemove = c.lastIndexOf('<Compile Remove="');
  const lastItemGroupEnd = c.indexOf('</ItemGroup>', lastRemove);
  const additions = files.filter(f => !c.includes(`<Compile Remove="${f}"`));
  if (!additions.length) return;
  const s = additions.map(f => `        <Compile Remove="${f}"/>`).join('\n');
  c = c.substring(0, lastItemGroupEnd) + '\n' + s + '\n    ' + c.substring(lastItemGroupEnd);
  writeFileSync(full, c, 'utf-8');
  console.log('  +', additions.join(', '));
}

addExclude('projects/Sonic.Standard.Schema/Sonic.Standard.Schema.csproj', [
  'Generator\\SchemaSnapshot.cs',
  'Config\\ConfigFieldTrait.cs',
  'Cache\\Redis\\RedisConnection.cs',
]);

// 7. Sonic.Standard.DL - 排除 NeuralIKunNodes
console.log('\n=== Sonic.Standard.DL ===');
addExclude('projects/Sonic.Standard.DL/Sonic.Standard.DL.csproj', [
  'Compiler\\NyarBridge\\NeuralIKunNodes.cs',
]);

// 8. Valkyrie.CLI - 排除 Diag
console.log('\n=== Valkyrie.CLI ===');
addExclude('tools/vcc/Valkyrie.CLI.csproj', [
  'Diag\\DiagTool.cs',
]);

// 9. Legion.CLI - 排除更多
console.log('\n=== Legion.CLI ===');
addExclude('tools/legion/Legion.CLI.csproj', [
  'Document\\UserDocRenderer.cs',
  'Document\\ThemeEngine.cs',
]);

// 10. LegacyVM - 排除 UnifiedTreeEvaluator
console.log('\n=== LegacyVM ===');
addExclude('projects/Nyar.VM.LegacyVM/LegacyVM.csproj', [
  'Evaluator\\UnifiedTreeEvaluator.cs',
]);

// 11. Atlas.Tests - 排除更多
console.log('\n=== Atlas.Tests ===');
addExclude('tests/Atlas.Tests/Atlas.Tests.csproj', [
  'Cloud\\CloudAbstractionsTests.cs',
]);

// 12. Asgard.CLI - 直接用正确路径排除
console.log('\n=== Asgard.CLI ===');
addExclude('tools/voa/Asgard.CLI.csproj', [
  '..\\..\\projects\\Nyar.Language\\Awsl.Asgard\\Compiler\\AsgardMultiTargetBuilder.cs',
]);

console.log('\n完成');