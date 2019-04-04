#!/usr/bin/env node

/**
 * 编译器发布脚本
 *
 * 将 VCC 和 legion 打包并发布到各包管理器：
 * - NuGet: dotnet pack + dotnet nuget push
 * - Maven: 未来支持（需要 IKVM 或 GraalVM）
 * - npm:   未来支持（需要 .NET WASM 发布）
 *
 * 用法：
 *   node scripts/publish-compiler.mjs [--target nuget] [--dry-run] [--version <version>]
 */

import fs from 'fs';
import path from 'path';
import { execSync } from 'child_process';

const SCRIPT_DIR = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]):\//, '$1:/'));
const ROOT_DIR = path.resolve(SCRIPT_DIR, '..');
const PACK_OUTPUT_DIR = path.join(ROOT_DIR, 'packages');

// ─────────────────────────────────────────────────────────────
// 工具配置
// ─────────────────────────────────────────────────────────────

const TOOLS = [
    {
        name: 'VCC (Valkyrie Compiler Collection)',
        csproj: 'tools/vcc/Valkyrie.CLI.csproj',
        packageId: 'Valkyrie.Compiler.CLR',
        toolCommand: 'vcc',
    },
    {
        name: 'Legion (构建工具链)',
        csproj: 'tools/legion/Legion.CLI.csproj',
        packageId: 'Valkyrie.Legion.CLR',
        toolCommand: 'legion',
    },
];

const NUGET_SOURCE = 'https://api.nuget.org/v3/index.json';

// ─────────────────────────────────────────────────────────────
// 工具函数
// ─────────────────────────────────────────────────────────────

function runCommand(command, options = {}) {
    try {
        const result = execSync(command, {
            encoding: 'utf8',
            timeout: options.timeout || 120000,
            cwd: options.cwd || ROOT_DIR,
            stdio: options.silent ? 'pipe' : 'inherit',
        });
        return { success: true, stdout: result };
    } catch (error) {
        if (options.allowFailure) {
            return { success: false, stdout: error.stdout || '', stderr: error.stderr || '' };
        }
        throw error;
    }
}

function readVersion(csprojPath) {
    const content = fs.readFileSync(csprojPath, 'utf8');
    const match = content.match(/<Version>(.*?)<\/Version>/);
    return match ? match[1] : '0.0.0';
}

function updateVersion(csprojPath, newVersion) {
    let content = fs.readFileSync(csprojPath, 'utf8');
    content = content.replace(/<Version>.*?<\/Version>/, `<Version>${newVersion}</Version>`);
    fs.writeFileSync(csprojPath, content, 'utf8');
}

// ─────────────────────────────────────────────────────────────
// NuGet 发布
// ─────────────────────────────────────────────────────────────

function publishToNuGet(dryRun, version) {
    console.log('\n=== NuGet 发布 ===\n');

    if (!fs.existsSync(PACK_OUTPUT_DIR)) {
        fs.mkdirSync(PACK_OUTPUT_DIR, { recursive: true });
    }

    for (const tool of TOOLS) {
        const csprojPath = path.join(ROOT_DIR, tool.csproj);
        if (!fs.existsSync(csprojPath)) {
            console.log(`跳过不存在的项目：${tool.name} (${tool.csproj})`);
            continue;
        }

        // 更新版本号
        if (version) {
            updateVersion(csprojPath, version);
            console.log(`${tool.name} 版本已更新为 ${version}`);
        }

        const currentVersion = readVersion(csprojPath);
        console.log(`\n--- ${tool.name} v${currentVersion} ---`);

        // dotnet pack
        const packCommand = `dotnet pack "${csprojPath}" -c Release --nologo -o "${PACK_OUTPUT_DIR}"`;
        console.log(`执行：${packCommand}`);

        if (!dryRun) {
            runCommand(packCommand);
        }

        // 检查 nupkg 是否生成
        const nupkgPattern = `${tool.packageId}.${currentVersion}.nupkg`;
        const nupkgPath = path.join(PACK_OUTPUT_DIR, nupkgPattern);

        if (dryRun) {
            console.log(`[dry-run] 将生成：${nupkgPattern}`);
            continue;
        }

        if (!fs.existsSync(nupkgPath)) {
            console.error(`错误：NuGet 包未生成：${nupkgPattern}`);
            continue;
        }

        console.log(`已生成：${nupkgPattern}`);

        // dotnet nuget push
        const pushCommand = `dotnet nuget push "${nupkgPath}" --source "${NUGET_SOURCE}" --skip-duplicate`;
        console.log(`执行：${pushCommand}`);

        try {
            runCommand(pushCommand);
            console.log(`${tool.name} 发布成功`);
        } catch (error) {
            console.error(`${tool.name} 发布失败：${error.message}`);
        }
    }
}

// ─────────────────────────────────────────────────────────────
// Maven 发布（未来支持）
// ─────────────────────────────────────────────────────────────

function publishToMaven(dryRun, version) {
    console.log('\n=== Maven 发布 ===\n');
    console.log('JVM 后端编译器发布尚未实现');
    console.log('需要：IKVM 编译或 GraalVM native-image');
    console.log('预期产物：dev.valkyrie:compiler-jvm');
    if (version) {
        console.log(`预期版本：${version}`);
    }
}

// ─────────────────────────────────────────────────────────────
// npm 发布（未来支持）
// ─────────────────────────────────────────────────────────────

function publishToNpm(dryRun, version) {
    console.log('\n=== npm 发布 ===\n');
    console.log('WASM 后端编译器发布尚未实现');
    console.log('需要：dotnet publish → WASM + JS 胶水代码');
    console.log('预期产物：@valkyrie/compiler-wasm');
    if (version) {
        console.log(`预期版本：${version}`);
    }
}

// ─────────────────────────────────────────────────────────────
// 主入口
// ─────────────────────────────────────────────────────────────

function parseArgs() {
    const args = process.argv.slice(2);
    const options = {
        target: 'nuget',
        dryRun: false,
        version: null,
    };

    for (let i = 0; i < args.length; i++) {
        switch (args[i]) {
            case '--target':
            case '-t':
                options.target = args[++i];
                break;
            case '--dry-run':
                options.dryRun = true;
                break;
            case '--version':
            case '-v':
                options.version = args[++i];
                break;
            case '--help':
            case '-h':
                console.log(`
编译器发布脚本

用法：node scripts/publish-compiler.mjs [选项]

选项：
  --target, -t <target>  发布目标：nuget, maven, npm, all（默认 nuget）
  --dry-run              仅模拟，不实际发布
  --version, -v <ver>    指定版本号
  --help, -h             显示帮助信息
`);
                process.exit(0);
        }
    }

    return options;
}

function main() {
    const options = parseArgs();

    console.log('╔══════════════════════════════════════════════════════════╗');
    console.log('║              Valkyrie 编译器发布工具                    ║');
    console.log('╚══════════════════════════════════════════════════════════╝\n');

    if (options.dryRun) {
        console.log('[dry-run 模式 — 仅模拟，不实际发布]\n');
    }

    const targets = options.target === 'all'
        ? ['nuget', 'maven', 'npm']
        : [options.target];

    for (const target of targets) {
        switch (target) {
            case 'nuget':
                publishToNuGet(options.dryRun, options.version);
                break;
            case 'maven':
                publishToMaven(options.dryRun, options.version);
                break;
            case 'npm':
                publishToNpm(options.dryRun, options.version);
                break;
            default:
                console.error(`未知发布目标：${target}`);
                console.error('可用目标：nuget, maven, npm, all');
                process.exit(1);
        }
    }

    console.log('\n发布流程完成');
}

main();
