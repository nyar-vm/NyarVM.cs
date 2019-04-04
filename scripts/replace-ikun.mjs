import { readFileSync, writeFileSync, readdirSync, statSync } from 'fs';
import { join, extname } from 'path';

const baseDir = 'e:/RiderProjects/NyarVM.cs/projects/Nyar';

function walk(dir) {
  const results = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    try {
      const st = statSync(full);
      if (st.isDirectory()) {
        if (entry === 'obj' || entry === 'bin') continue;
        results.push(...walk(full));
      } else if (extname(entry) === '.cs') {
        results.push(full);
      }
    } catch { /* skip */ }
  }
  return results;
}

const files = walk(baseDir);
let count = 0;

for (const f of files) {
  const content = readFileSync(f, 'utf-8');
  if (!content.includes('IKun')) continue;
  // 先替换复合词
  let replaced = content.replace(/\bIKunTree\b/g, 'OaTree');
  replaced = replaced.replace(/\bIKunNode\b/g, 'OaNode');
  // 再替换单独词
  replaced = replaced.replace(/\bIKun\b/g, 'Oa');
  if (replaced !== content) {
    writeFileSync(f, replaced, 'utf-8');
    console.log(f.replace(baseDir, ''));
    count++;
  }
}
console.log(`\n完成: ${count} 个文件`);