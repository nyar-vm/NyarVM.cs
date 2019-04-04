import { readFileSync } from 'fs';

const content = readFileSync('e:/RiderProjects/NyarVM.cs/build_output2.txt', 'utf-8');
const lines = content.split('\n').filter(l => l.includes('error CS'));

const byProject = {};
for (const line of lines) {
  const pm = line.match(/\[([^\]]+\.csproj)\]/);
  if (pm) {
    const proj = pm[1].split('\\').pop();
    byProject[proj] = (byProject[proj] || 0) + 1;
  }
}

const sorted = Object.entries(byProject).sort((a, b) => b[1] - a[1]);
console.log(`Total CS errors: ${lines.length}`);
console.log('');
for (const [proj, count] of sorted) {
  console.log(`${proj}: ${count}`);
}

// Show first error for each project
console.log('\n--- Samples ---');
const seen = new Set();
for (const line of lines) {
  const pm = line.match(/\[([^\]]+\.csproj)\]/);
  if (pm) {
    const proj = pm[1].split('\\').pop();
    if (!seen.has(proj)) {
      seen.add(proj);
      // Extract just the error part
      const errMatch = line.match(/error CS\d+:[^[]*/);
      const filePart = line.match(/([^\\]+\.cs\(\d+)/);
      console.log(`\n${proj}:`);
      console.log(`  ${filePart ? filePart[1] : ''}: ${errMatch ? errMatch[0].trim() : line.trim().substring(0,150)}`);
    }
  }
  if (seen.size >= 15) break;
}