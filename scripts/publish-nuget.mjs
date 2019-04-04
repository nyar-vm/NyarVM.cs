#!/usr/bin/env node

import fs from 'fs';
import path from 'path';
import { execSync } from 'child_process';

const SCRIPT_DIR = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]):\//, '$1:/'));
const ROOT_DIR = path.resolve(SCRIPT_DIR, '..');
const DIR_BUILD_PROPS_PATH = path.join(ROOT_DIR, 'Directory.Build.props');
const PACK_OUTPUT_DIR = path.join(ROOT_DIR, 'packages');

const CONFIG = {
  packagesToPublish: [
    'Hypersonic',
    'Sonic',
    'Olympus.Atlas'
  ],
  nugetSource: 'https://api.nuget.org/v3/index.json'
};

let sharedVersion;
let allProjects = [];

function loadSharedVersion() {
  if (fs.existsSync(DIR_BUILD_PROPS_PATH)) {
    const content = fs.readFileSync(DIR_BUILD_PROPS_PATH, 'utf8');
    const versionMatch = content.match(/<Version>(.*?)<\/Version>/);
    if (versionMatch) {
      sharedVersion = versionMatch[1].trim();
      console.log(`\x1b[36m[CONFIG]\x1b[0m 共享版本号: ${sharedVersion}`);
    }
  }
}

function readProjectFile(projectPath) {
  const content = fs.readFileSync(projectPath, 'utf8');

  const packageIdMatch = content.match(/<PackageId>(.*?)<\/PackageId>/);
  const packageId = packageIdMatch ? packageIdMatch[1].trim() : null;

  const versionMatch = content.match(/<Version>(.*?)<\/Version>/);
  let version = versionMatch ? versionMatch[1].trim() : null;
  
  if (!version && sharedVersion) {
    version = sharedVersion;
  }

  const dependencies = [];
  const projectReferenceMatches = content.match(/<ProjectReference[^>]+Include="([^"]+)"/g);
  if (projectReferenceMatches) {
    projectReferenceMatches.forEach(match => {
      const includeMatch = match.match(/Include="([^"]+)"/);
      if (includeMatch) {
        const relativePath = includeMatch[1];
        const absolutePath = path.resolve(path.dirname(projectPath), relativePath);
        dependencies.push(absolutePath);
      }
    });
  }

  return {
    path: projectPath,
    packageId,
    version,
    dependencies
  };
}

function findPackageByPath(pathToFind) {
  return allProjects.find(p => p.path === pathToFind);
}

function findPackageById(packageId) {
  return allProjects.find(p => p.packageId === packageId);
}

function sortByDependencies(packages) {
  const result = [];
  const visited = new Set();
  
  function visit(packageId) {
    if (visited.has(packageId)) return;
    visited.add(packageId);
    
    const project = findPackageById(packageId);
    if (!project) return;
    
    for (const depPath of project.dependencies) {
      const depProject = findPackageByPath(depPath);
      if (depProject && depProject.packageId && CONFIG.packagesToPublish.includes(depProject.packageId)) {
        visit(depProject.packageId);
      }
    }
    
    result.push(packageId);
  }
  
  packages.forEach(pkg => visit(pkg));
  return result;
}

function buildProject(project) {
  console.log(`\x1b[33m[BUILD]\x1b[0m ${project.packageId} v${project.version}`);
  const projectDir = path.dirname(project.path);
  
  execSync(`dotnet pack -c Release --nologo -o "${PACK_OUTPUT_DIR}" --force`, { cwd: projectDir, stdio: 'inherit' });
}

function publishProject(project) {
  console.log(`\x1b[34m[PUBLISH]\x1b[0m ${project.packageId} v${project.version}`);
  
  const nupkgPattern = `${project.packageId}.${project.version}.nupkg`;
  
  if (!fs.existsSync(PACK_OUTPUT_DIR)) {
    console.error(`\x1b[31m[ERROR]\x1b[0m 输出目录不存在: ${PACK_OUTPUT_DIR}`);
    return false;
  }
  
  const nupkgFiles = fs.readdirSync(PACK_OUTPUT_DIR).filter(f => f === nupkgPattern);
  
  if (nupkgFiles.length === 0) {
    console.error(`\x1b[31m[ERROR]\x1b[0m NuGet 包未找到: ${nupkgPattern}`);
    return false;
  }

  const nupkgPath = path.join(PACK_OUTPUT_DIR, nupkgFiles[0]);
  
  try {
    execSync(`dotnet nuget push "${nupkgPath}" --source "${CONFIG.nugetSource}" --skip-duplicate`, { 
      stdio: 'inherit',
      cwd: PACK_OUTPUT_DIR 
    });
    console.log(`\x1b[32m[SUCCESS]\x1b[0m ${project.packageId} 发布成功`);
    return true;
  } catch (error) {
    console.error(`\x1b[31m[ERROR]\x1b[0m ${project.packageId} 发布失败: ${error.message}`);
    return false;
  }
}

async function main() {
  console.log('\x1b[36m=== Olympus NuGet 自动化发布工具 ===\x1b[0m\n');

  try {
    loadSharedVersion();

    const projectsDir = path.join(ROOT_DIR, 'projects');
    console.log(`\x1b[36m[CONFIG]\x1b[0m 项目目录: ${projectsDir}`);

    const csprojFiles = [];
    function findCsproj(dir) {
      const files = fs.readdirSync(dir);
      for (const file of files) {
        const fullPath = path.join(dir, file);
        const stat = fs.statSync(fullPath);
        if (stat.isDirectory() && !file.startsWith('.')) {
          findCsproj(fullPath);
        } else if (file.endsWith('.csproj')) {
          csprojFiles.push(fullPath);
        }
      }
    }

    findCsproj(projectsDir);
    console.log(`\x1b[36m[SCAN]\x1b[0m 找到 ${csprojFiles.length} 个项目文件\n`);

    allProjects = csprojFiles.map(readProjectFile).filter(p => p.packageId && p.version);
    console.log(`\x1b[36m[SCAN]\x1b[0m 解析出 ${allProjects.length} 个有效项目\n`);

    const sortedPackageIds = sortByDependencies(CONFIG.packagesToPublish);
    const packagesToPublish = sortedPackageIds.map(id => findPackageById(id)).filter(Boolean);

    console.log(`\x1b[35m待发布包（${packagesToPublish.length} 个，按依赖排序）:\x1b[0m`);
    packagesToPublish.forEach((p, index) => console.log(`${index + 1}. ${p.packageId} v${p.version}`));
    console.log('');

    if (!fs.existsSync(PACK_OUTPUT_DIR)) {
      fs.mkdirSync(PACK_OUTPUT_DIR, { recursive: true });
    }

    for (const project of packagesToPublish) {
      console.log(`\n\x1b[36m=== 处理 ${project.packageId} ===\x1b[0m`);
      buildProject(project);
      const success = publishProject(project);
      if (!success) {
        console.log(`\x1b[31m=== ${project.packageId} 失败，终止发布 ===\x1b[0m`);
        process.exit(1);
      }
      console.log(`\x1b[36m=== ${project.packageId} 完成 ===\x1b[0m`);
    }

    console.log('\n\x1b[32m=== 🎉 所有包发布完成! ===\x1b[0m');

  } catch (error) {
    console.error('\x1b[31m[ERROR]\x1b[0m', error.message);
    process.exit(1);
  }
}

main();