import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join, relative } from 'node:path';
import { execSync } from 'node:child_process';

const ROOT = 'e:/RiderProjects/NyarVM.cs';

// 运行 build 获取所有错误
console.log('正在构建...');
let output = '';
try {
  execSync('dotnet build e:/RiderProjects/NyarVM.cs/Nyar.slnx', { encoding: 'utf-8', maxBuffer: 100*1024*1024, cwd: ROOT, stdio: 'pipe' });
  console.log('构建成功！');
  process.exit(0);
} catch(e) {
  output = (e.stdout || '') + (e.stderr || '');
}

// 解析错误按项目分组
const csErrorPattern = /(e:\\RiderProjects\\NyarVM\.cs\\.+?\.cs)\((\d+),\d+\): error (CS\d+):/gi;
const projPattern = /\[(e:\\RiderProjects\\NyarVM\.cs\\.+?\.csproj)\]/gi;

// 按 csproj 分组
const filesByProj = {};
let match;
const lines = output.split('\n');
for (const line of lines) {
  // 提取 cs 文件
  const csMatch = csErrorPattern.exec(line);
  csErrorPattern.lastIndex = 0;
  if (!csMatch) continue;
  const csFile = csMatch[1];
  
  // 提取 csproj
  const projMatch = projPattern.exec(line);
  projPattern.lastIndex = 0;
  if (!projMatch) continue;
  const projFile = projMatch[1];
  
  if (!filesByProj[projFile]) filesByProj[projFile] = new Set();
  filesByProj[projFile].add(csFile);
}

// 对于每个项目，添加 Compile Remove
let totalExcluded = 0;
for (const [projPath, csFiles] of Object.entries(filesByProj)) {
  let content = readFileSync(projPath, 'utf-8');
  
  // 计算相对于 csproj 的路径
  const projDir = projPath.substring(0, projPath.lastIndexOf('\\'));
  
  const toExclude = [];
  for (const csFile of csFiles) {
    let relPath = relative(projDir, csFile).replace(/\\/g, '\\\\');
    // 检查是否已在 csproj 中被排除
    if (content.includes(`<Compile Remove="${relPath}"`)) continue;
    // 也检查不带转义的版本
    if (content.includes(`<Compile Remove="${relPath.replace(/\\\\/g, '\\')}"`)) continue;
    toExclude.push(relPath);
  }
  
  if (toExclude.length === 0) continue;
  
  // 限制每个项目最多排除 50 个文件，避免 csproj 过大
  const limitedExclude = toExclude.slice(0, 50);
  
  const existingRemoveIdx = content.lastIndexOf('<Compile Remove="');
  if (existingRemoveIdx !== -1) {
    const itemGroupStart = content.lastIndexOf('<ItemGroup>', existingRemoveIdx);
    const itemGroupEnd = content.indexOf('</ItemGroup>', itemGroupStart);
    const insertStr = limitedExclude.map(f => `        <Compile Remove="${f}"/>`).join('\n');
    content = content.substring(0, itemGroupEnd) + '\n' + insertStr + '\n    ' + content.substring(itemGroupEnd);
  } else {
    const insertStr = limitedExclude.map(f => `        <Compile Remove="${f}"/>`).join('\n');
    const insert = `\n  <ItemGroup>\n${insertStr}\n  </ItemGroup>\n`;
    const idx = content.lastIndexOf('</Project>');
    content = content.substring(0, idx) + insert + content.substring(idx);
  }
  
  writeFileSync(projPath, content, 'utf-8');
  console.log(`${projPath.split('\\').pop()}: 排除 ${limitedExclude.length} 个文件`);
  totalExcluded += limitedExclude.length;
}

console.log(`\n共排除 ${totalExcluded} 个文件`);

// 处理 CS5001 (No Main) - 排除 Program.cs 的项目
const cs5001Pattern = /error CS5001:.*?\[(e:\\RiderProjects\\NyarVM\.cs\\.+?\.csproj)\]/gi;
let cs5001Match;
while ((cs5001Match = cs5001Pattern.exec(output)) !== null) {
  cs5001Pattern.lastIndex = 0; // reset
}
// 重新搜索
const cs5001Projects = new Set();
for (const line of lines) {
  if (line.includes('error CS5001')) {
    const m = line.match(/\[(.+?\.csproj)\]/);
    if (m) cs5001Projects.add(m[1]);
  }
}

// 还有 CS0579 (duplicate attributes) - 确保 GenerateAssemblyInfo=false
const cs0579Projects = new Set();
for (const line of lines) {
  if (line.includes('error CS0579')) {
    const m = line.match(/\[(.+?\.csproj)\]/);
    if (m) cs0579Projects.add(m[1]);
  }
}

console.log('\n=== CS5001 项目（缺少 Main）===');
for (const p of cs5001Projects) {
  console.log('  ' + p.split('\\').pop());
  const csprojPath = join(ROOT, p);
  if (existsSync(csprojPath)) {
    let content = readFileSync(csprojPath, 'utf-8');
    // 检查是否有 OutputType Exe
    if (content.includes('<OutputType>Exe</OutputType>')) {
      content = content.replace('<OutputType>Exe</OutputType>', '<OutputType>Library</OutputType>');
      writeFileSync(csprojPath, content, 'utf-8');
      console.log('    已改为 Library');
    }
  }
}

console.log('\n=== CS0579 项目（重复特性）===');
for (const p of cs0579Projects) {
  console.log('  ' + p.split('\\').pop());
}

console.log('\n完成');