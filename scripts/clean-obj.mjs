import { readdirSync, rmSync, existsSync } from 'fs';
import { join } from 'path';

const ROOT = 'e:/RiderProjects/NyarVM.cs';

/// 递归查找并删除 obj 和 bin 目录
function cleanDir(dir) {
  const entries = readdirSync(dir, { withFileTypes: true });
  for (const e of entries) {
    if (!e.isDirectory()) continue;
    if (e.name === 'obj' || e.name === 'bin') {
      const p = join(dir, e.name);
      try {
        rmSync(p, { recursive: true, force: true });
        console.log('已删除:', p);
      } catch (ex) {
        console.log('删除失败:', p, ex.message);
      }
    } else {
      cleanDir(join(dir, e.name));
    }
  }
}

cleanDir(ROOT);
console.log('清理完成');