// Standalone report smoke test with a minimal DOM; no npm packages required.
const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
class Element {
  constructor(tag) { this.tag = tag; this.children = []; this.style = {}; this.value = 'tag'; this.scrollWidth = 1000; }
  append(...items) { this.children.push(...items); }
  replaceChildren(...items) { this.children = items; }
  setAttribute() {}
  addEventListener(event, callback) { this[event] = callback; }
}
const controls = Object.fromEntries(['#group', '#status', '#charts'].map(id => [id, new Element('div')]));
const RealDate = Date;
class FixedDate extends RealDate { constructor(...args) { super(...(args.length ? args : ['2026-11-10T12:00:00'])); } }
const context = vm.createContext({
  Date: FixedDate,
  document: { querySelector: id => controls[id], createElement: tag => new Element(tag), createTextNode: text => ({ textContent: text }) },
  requestAnimationFrame: callback => callback(), setInterval: () => {}
});
const entries = [
  {date:'2026-11-03',start:'2026-11-03T23:00:00',end:'2026-11-04T02:00:00',name:'Munka: A'},
  {date:'2026-11-04',start:'2026-11-04T08:00:00',end:'2026-11-04T08:30:00',name:'Munka: B'},
  {date:'2026-11-10',start:'2026-11-10T11:00:00',end:null,name:'Pihenés: <img src=x onerror=alert(1)>'}
];
const html = fs.readFileSync('src/Ora.Core/report.html','utf8');
const script = html.slice(html.indexOf('<script>') + 8, html.lastIndexOf('</script>')).replace('/*DATA*/[]',JSON.stringify(entries));
vm.runInContext(script, context);
function descendants(node, tag) { return [ ...(node.tag === tag ? [node] : []), ...(node.children || []).flatMap(child => descendants(child, tag)) ]; }
const sections = controls['#charts'].children;
assert.equal(sections.length, 3, 'daily, weekly and monthly charts');
const dailyRows = descendants(sections[0], 'tr').slice(1).map(row => row.children.map(cell => cell.textContent));
assert(dailyRows.some(row => row[0] === '2026-11-03' && row[1] === 'Munka' && row[2] === '1 ó 0 p 0 mp'));
assert(dailyRows.some(row => row[0] === '2026-11-04' && row[1] === 'Munka' && row[2] === '2 ó 30 p 0 mp'));
assert(dailyRows.some(row => row[0] === '2026-11-10' && row[2] === '1 ó 0 p 0 mp'), 'open session included');
assert.equal(vm.runInContext("period('2026-11-08','week')", context), '2026-11-02');
assert.equal(vm.runInContext("advance('2026-12-01','month')", context), '2027-01-01');
controls['#group'].value = 'name';
controls['#group'].change();
const rowsByName = descendants(controls['#charts'].children[0], 'tr').slice(1).map(row => row.children.map(cell => cell.textContent));
assert(rowsByName.some(row => row[1] === 'Munka: B' && row[2] === '0 ó 30 p 0 mp'), 'group switch separates descriptions');
assert(rowsByName.some(row => row[1] === entries[2].name), 'HTML-looking name retained as text');
assert.equal(descendants(controls['#charts'], 'img').length, 0, 'names create no HTML elements');
console.log('Report checks passed: daily splits, group totals, live session, ISO week, year boundary, grouping switch, safe text.');
