import { execSync } from 'child_process';

try { execSync('dotnet build e:/RiderProjects/NyarVM.cs/Nyar.slnx', { encoding: 'utf-8', maxBuffer: 100*1024*1024, cwd: 'e:/RiderProjects/NyarVM.cs' }); }
catch(e) {
  const lines = (e.stdout+e.stderr).split('\n').filter(l => l.includes('error CS'));
  const byProj = {};
  for (const l of lines) {
    const m = l.match(/\[(.+?\.csproj)\]/);
    if (m) { 
      const p = m[1].split('\\').pop(); 
      if (!byProj[p]) byProj[p] = [];
      if (byProj[p].length < 3) byProj[p].push(l.trim().substring(0, 200)); 
    }
  }
  for (const [p,errs] of Object.entries(byProj).sort((a,b) => b[1] - a[1])) {
    console.log('=== ' + p + ' (' + errs.length + ') ===');
    for (const s of errs) console.log('  ' + s);
  }
  console.log('Total: ' + lines.length);
}