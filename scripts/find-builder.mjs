import { readdirSync, readFileSync, existsSync, statSync } from "fs";
import { join } from "path";

function findFile(dir, pattern) {
    if (!existsSync(dir)) return [];
    const results = [];
    try {
        for (const entry of readdirSync(dir)) {
            const fullPath = join(dir, entry);
            const stat = statSync(fullPath);
            if (stat.isDirectory()) {
                results.push(...findFile(fullPath, pattern));
            } else if (entry.includes(pattern)) {
                results.push(fullPath);
            }
        }
    } catch (e) {}
    return results;
}

const objDir = "e:/RiderProjects/NyarVM.cs/projects/Nyar.Core/obj";
const files = findFile(objDir, "IKunBuilder");
console.log(`找到 ${files.length} 个文件:`);
files.forEach(f => console.log(f));

if (files.length > 0) {
    const content = readFileSync(files[0], "utf8");
    const methods = content.match(/public static Id \w+\(/g) || [];
    const newMethods = methods.filter(m => m.includes("Attributes") || m.includes("Match(") || m.includes("Catch(") || m.includes("Resume("));
    console.log("\n新方法:", newMethods);
}
