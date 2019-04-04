import { execSync } from 'child_process';

const result = execSync('dotnet build Nyar.slnx --no-restore', {
  cwd: 'e:/RiderProjects/NyarVM.cs',
  maxBuffer: 50 * 1024 * 1024,
  encoding: 'utf-8',
  stdio: ['pipe', 'pipe', 'pipe']
}).stdout + execSync('dotnet build Nyar.slnx --no-restore 2>&1', {
  cwd: 'e:/RiderProjects/NyarVM.cs',
  maxBuffer: 50 * 1024 * 1024,
  encoding: 'utf-8',
  stdio: ['pipe', 'pipe', 'pipe']
}).stderr;

const lines = result.split('\n').filter(l => l.includes('error CS'));
const byProject = {};

for (const line of lines) {
  const m = line.match(/\[([^\]]+\.csproj)\]/);
  if (m) {
    const proj = m[1].split('\\').pop();
    byProject[proj] = (byProject[proj] || 0) + 1;
  }
}

const sorted = Object.entries(byProject).sort((a, b) => b[1] - a[1]);
console.log(`Total errors: ${lines.length}`);
console.log('');
for (const [proj, count] of sorted) {
  console.log(`${proj}: ${count}`);
}