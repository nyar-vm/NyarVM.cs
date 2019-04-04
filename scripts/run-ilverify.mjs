#!/usr/bin/env node

/**
 * 运行 ILVerify 验证 CLR 产物的 IL 正确性
 * 使用最小必要引用集避免崩溃
 *
 * 用法：
 *   node scripts/run-ilverify.mjs <exe-or-dll-path> [--runtime <version>]
 */

import { execFileSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import os from 'os';

const args = process.argv.slice(2);
if (args.length === 0 || args.includes('--help')) {
    console.log('usage: node run-ilverify.mjs <exe-path> [--runtime <version>]');
    process.exit(0);
}

const exePath = path.resolve(args[0]);
let runtimeVersion = '10.0.6';

const runtimeIdx = args.indexOf('--runtime');
if (runtimeIdx >= 0 && args[runtimeIdx + 1]) {
    runtimeVersion = args[runtimeIdx + 1];
}

const runtimeDir = path.join('C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App', runtimeVersion);

if (!fs.existsSync(runtimeDir)) {
    console.error(`runtime dir not found: ${runtimeDir}`);
    process.exit(2);
}

// 只加载必要的引用 - System.Private.CoreLib 是系统模块
// 其他引用通过它来解析
const refs = [
    'System.Private.CoreLib.dll',
    'System.Runtime.dll',
    'System.Console.dll',
    'System.Runtime.Extensions.dll',
    'System.IO.FileSystem.dll',
    'System.ObjectModel.dll',
    'System.Collections.dll',
    'System.Collections.NonGeneric.dll',
];

const refPaths = refs.map(r => path.join(runtimeDir, r));

console.log(`target: ${exePath}`);
console.log(`runtime: ${runtimeVersion}`);

// 使用 execFileSync 直接传递参数数组
const ilverifyArgs = [exePath];
for (const rp of refPaths) {
    ilverifyArgs.push('-r', rp);
}
ilverifyArgs.push('-s', 'System.Private.CoreLib');

let output = '';
try {
    output = execFileSync('ilverify', ilverifyArgs, {
        encoding: 'utf8',
        timeout: 120000,
        maxBuffer: 50 * 1024 * 1024,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: false,
    });
} catch (error) {
    output = (error.stdout || '') + (error.stderr || '');
}

// write raw output to file for debugging
const outFile = path.join(os.tmpdir(), 'ilverify-raw.txt');
fs.writeFileSync(outFile, output, 'utf8');
console.log(`raw output: ${outFile} (${output.length} bytes)`);

const lines = output.split('\n').filter(l => l.trim().length > 0);

// count [IL]: Error lines
const errorLines = lines.filter(l => l.includes('[IL]: Error'));
console.log(`\n=== ILVerify Results ===`);
console.log(`total [IL]: Error lines: ${errorLines.length}`);

if (errorLines.length > 0) {
    // extract error types
    const errorTypes = {};
    for (const line of errorLines) {
        const match = line.match(/Error \[(\S+?)\]/);
        const errType = match ? match[1] : 'UnresolvedToken';
        errorTypes[errType] = (errorTypes[errType] || 0) + 1;
    }
    console.log('\nerror breakdown:');
    for (const [type, count] of Object.entries(errorTypes).sort((a, b) => b[1] - a[1])) {
        console.log(`  ${type}: ${count}`);
    }

    // show first 40 errors grouped by type
    console.log('\nfirst 40 errors:');
    errorLines.slice(0, 40).forEach((line, i) => {
        // shorten the path
        const short = line.replace(/e:[^"]*legion\.exe/, 'legion.exe');
        console.log(`  ${i + 1}. ${short.trim()}`);
    });
}

// check for crash
if (output.includes('InvalidCastException') || output.includes('Unhandled exception')) {
    console.log('\n!! ILVerify crashed during verification !!');
    const crashLines = lines.filter(l =>
        l.includes('InvalidCastException') ||
        l.includes('Unhandled exception') ||
        l.includes('at Internal.')
    );
    crashLines.slice(0, 5).forEach(l => console.log(`  ${l.trim()}`));
}

process.exit(0);
