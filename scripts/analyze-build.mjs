import { readFileSync } from 'fs';

const content = readFileSync('e:/RiderProjects/NyarVM.cs/scripts/build-output.txt', 'utf-8');
const lines = content.split('\n');

const errorLines = lines.filter(l => l.includes('error CS'));
const projErrors = {};
for (const line of errorLines) {
  const match = line.match(/error CS\d+.*?\[(.*?)\]/);
  if (match) {
    const proj = match[1].replace(/.*\\/, '');
    projErrors[proj] = (projErrors[proj] || 0) + 1;
  }
}

const sorted = Object.entries(projErrors).sort((a, b) => b[1] - a[1]);
for (const [proj, count] of sorted) {
  console.log(`${count.toString().padStart(4)}  ${proj}`);
}
console.log(`\nTotal CS errors: ${errorLines.length}`);