import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, resolve, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = resolve(join(__dirname, '..'));

function walk(dir, ext) {
    const results = [];
    try {
        const entries = readdirSync(dir, { withFileTypes: true });
        for (const entry of entries) {
            const full = join(dir, entry.name);
            if (entry.isDirectory()) {
                if (['node_modules', '.git', 'bin', 'obj'].includes(entry.name)) continue;
                results.push(...walk(full, ext));
            }
            else if (entry.isFile() && entry.name.endsWith(ext)) {
                results.push(full);
            }
        }
    }
    catch (e) { /* ignore */ }
    return results;
}

const userDirs = [
    'projects/Nyar', 'projects/Nyar.Language', 'projects/Nyar.PackageManager',
    'projects/Nyar.PackageRegistry', 'projects/Nyar.SourceGenerator',
    'projects/Nyar.VM.LegacyVM', 'projects/Nyar.VM.NyarVM',
    'projects/Olympus.Hermes', 'projects/Sonic.Animator', 'projects/Sonic.Core',
    'projects/Sonic.Database', 'projects/Sonic.Plotter', 'projects/Sonic.SourceGenerator',
    'projects/Sonic.Standard', 'projects/Sonic.Standard.Application.Client',
    'projects/Sonic.Standard.Application.Server', 'projects/Sonic.Standard.Audio',
    'projects/Sonic.Standard.Command', 'projects/Sonic.Standard.Compression',
    'projects/Sonic.Standard.Data.Binary', 'projects/Sonic.Standard.Data.Binary.Clr',
    'projects/Sonic.Standard.Data.Binary.Jvm', 'projects/Sonic.Standard.Data.Binary.Wasm',
    'projects/Sonic.Standard.Data.Protocol', 'projects/Sonic.Standard.Data.Text',
    'projects/Sonic.Standard.Data.Text.Awsl', 'projects/Sonic.Standard.Data.Text.Valkyrie',
    'projects/Sonic.Standard.Data.Text.Wat', 'projects/Sonic.Standard.Data.Text.Wit',
    'projects/Sonic.Standard.DL', 'projects/Sonic.Standard.Document',
    'projects/Sonic.Standard.Image', 'projects/Sonic.Standard.Net',
    'projects/Sonic.Standard.Schema', 'projects/Sonic.Standard.Schema.ORM',
    'projects/Sonic.Standard.Template', 'projects/Sonic.Standard.Terminal',
    'projects/Sonic.Standard.Video',
];

for (const dir of userDirs) {
    const fullDir = join(root, dir);
    if (!existsSync(fullDir)) continue;
    const csprojFiles = walk(fullDir, '.csproj');
    for (const csproj of csprojFiles) {
        const content = readFileSync(csproj, 'utf-8');
        const baseDir = dirname(csproj);
        let hasBroken = false;
        for (const m of content.matchAll(/<ProjectReference\s+Include="([^"]+)"/g)) {
            const refPath = join(baseDir, m[1]);
            const normalized = resolve(refPath);
            if (!existsSync(normalized)) {
                if (!hasBroken) {
                    console.log(`\n❌ ${relative(root, csproj)}`);
                    hasBroken = true;
                }
                console.log(`   引用缺失: ${m[1]}`);
            }
        }
    }
}