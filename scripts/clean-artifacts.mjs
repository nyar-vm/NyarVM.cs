import { readdirSync, rmSync } from 'fs';
import { dirname, join, resolve } from 'path';
import { fileURLToPath } from 'url';

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(SCRIPT_DIR, '..');

let deletedCount = 0;

/// 递归查找并删除 `.artifacts` 目录
function cleanArtifacts(dir) {
  const entries = readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory()) {
      continue;
    }

    const fullPath = join(dir, entry.name);
    if (entry.name === '.artifacts') {
      try {
        rmSync(fullPath, { recursive: true, force: true });
        deletedCount += 1;
        console.log('已删除:', fullPath);
      } catch (error) {
        console.log('删除失败:', fullPath, error.message);
      }
      continue;
    }

    cleanArtifacts(fullPath);
  }
}

cleanArtifacts(ROOT);
console.log(`清理完成，共删除 ${deletedCount} 个 .artifacts 目录`);
