import { mkdirSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = resolve(root, 'docs/week-04/postman');
mkdirSync(output, { recursive: true });
const variable = name => `{{${name}}}`;
const folders = [];
let items;
function folder(name) { items = []; folders.push({ name, item: items }); }
function request(name, method, path, role, status, body, extra = '', options = {}) {
  let tests = `pm.test('HTTP ${status}', () => pm.response.to.have.status(${status}));`;
  if (options.json !== false && status !== 204) tests += '\nconst data = pm.response.json();';
  if (status >= 400) tests += `\npm.test('ProblemDetails', () => { pm.expect(pm.response.headers.get('Content-Type')).to.include('application/problem+json'); pm.expect(data.status).to.eql(${status}); pm.expect(data.code).to.be.a('string'); pm.expect(data.traceId).to.be.a('string'); });`;
  if (extra) tests += '\n' + extra;
  const result = {
    name,
    request: {
      method, url: variable('baseUrl') + path,
      auth: role ? { type: 'bearer', bearer: [{ key: 'token', value: variable(role + 'AccessToken'), type: 'string' }] } : { type: 'noauth' },
      header: [...(body ? [{ key: 'Content-Type', value: 'application/json' }] : []), ...(options.headers ?? [])],
      ...(body ? { body: { mode: 'raw', raw: JSON.stringify(body, null, 2), options: { raw: { language: 'json' } } } } : {})
    },
    event: [{ listen: 'test', script: { type: 'text/javascript', exec: ('(function () {\n'+tests+'\n})();').split('\n') } }]
  };
  if (options.before) result.event.unshift({ listen: 'prerequest', script: { type: 'text/javascript', exec: ('(function () {\n'+options.before+'\n})();').split('\n') } });
  items.push(result);
}
const saveId = name => `pm.environment.set('${name}', data.id);`;
const token = role => `pm.environment.set('${role}AccessToken', data.accessToken); pm.environment.set('${role}RefreshToken', data.refreshToken); pm.test('tokens received', () => { pm.expect(data.accessToken).to.be.a('string'); pm.expect(data.refreshToken).to.be.a('string'); });`;
const block = { Code: variable('operatorCode'), Name: 'Postman test block', Bays: 7, Rows: 1, Tiers: 2, IsVirtual: false };
const order = (number = 'orderNumber', expiry = 'futureExpiry') => ({ OrderNumber: variable(number), LineOperator: variable('operatorCode'), CustomerTaxCode: variable('customerTaxCode'), CustomerName: 'Postman test customer', OutboundVessel: 'Postman test vessel', ExpiryDate: variable(expiry), Items: [{ Size: 20, Type: 85, Quantity: 3 }] });
const registration = number => ({ ContainerNumber: variable(number), IsoCode: '22G1', Size: 20, Type: 85, TareWeight: 2000, MaxGrossWeight: 30000 });
const gateOut = (n, orderId = 'orderId') => ({ ContainerId: variable('containerId' + n), DeliveryOrderId: variable(orderId), OutVehicleNumber: variable('vehiclePlate'), VehicleType: 0 });
const position = { BlockId: variable('blockId'), Bay: 1, Row: 1, Tier: 1 };
const initialize = `
if (!pm.environment.get('adminPassword') || !pm.environment.get('testUserPassword')) throw new Error('Fill adminPassword and testUserPassword in your LOCAL environment first');
const suffix = Date.now().toString(36).slice(-6) + Math.random().toString(36).slice(2,4);
pm.environment.set('runSuffix', suffix);
pm.environment.set('operatorCode', ('P' + suffix).toUpperCase());
const ownerCode = Array.from({length:3},()=>String.fromCharCode(65+Math.floor(Math.random()*26))).join('');
pm.environment.set('ownerCode', ownerCode);
pm.environment.set('orderNumber', 'DO-' + suffix);
pm.environment.set('expiredOrderNumber', 'EXPIRED-' + suffix);
pm.environment.set('fortyOrderNumber', 'FORTY-' + suffix);
pm.environment.set('reentryOrderNumber', 'REENTRY-' + suffix);
pm.environment.set('customerTaxCode', String(Math.floor(1000000000 + Math.random() * 9000000000)));
pm.environment.set('vehiclePlate', '51C' + suffix.toUpperCase());
pm.environment.set('futureExpiry', new Date(Date.now() + 7 * 86400000).toISOString());
pm.environment.set('pastExpiry', new Date(Date.now() - 86400000).toISOString());
for (const role of ['yard','order','gate']) pm.environment.set(role + 'UserName', role + suffix);
for (const key of ['blockId','orderId','expiredOrderId','previousGateRefreshToken']) pm.environment.unset(key);
for (const role of ['admin','yard','order','gate']) { pm.environment.unset(role+'AccessToken'); pm.environment.unset(role+'RefreshToken'); }
const serial = Math.floor(100000 + Math.random() * 899990);
for (let n=1; n<=6; n++) {
  const raw = ownerCode + 'U' + String(serial+n).padStart(6,'0');
  let sum=0;
  for (let i=0; i<10; i++) {
    let value = /[0-9]/.test(raw[i]) ? Number(raw[i]) : raw.charCodeAt(i)-65+10;
    if (/[A-Z]/.test(raw[i])) { if(value>=11)value++; if(value>=22)value++; if(value>=33)value++; }
    sum += value * 2**i;
  }
  pm.environment.set('containerNumber'+n, raw + (sum%11%10));
  pm.environment.unset('containerId'+n); pm.environment.unset('slotId'+n);
}`.trim();

folder('01 - Initialize and Admin setup');
request('Initialize a unique test run - Liveness', 'GET', '/health/live', null, 200, null, '', { json: false, before: initialize });
request('Admin login', 'POST', '/api/auth/login', null, 200, { UserName: variable('adminUserName'), Password: variable('adminPassword') }, token('admin'));
request('Admin readiness', 'GET', '/health/ready', 'admin', 200, null, `pm.test('ready or optional degraded', () => pm.expect(['Healthy','Degraded']).to.include(data.status));`);
request('Create shared line operator', 'POST', '/api/line-operators', 'admin', 201, { Code: variable('operatorCode'), Name: 'Postman test operator' }, saveId('operatorId'));
request('Map container owner prefix', 'POST', '/api/line-operators/{{operatorId}}/owner-codes', 'admin', 204, { OwnerCode: variable('ownerCode') });
request('Create shared customer', 'POST', '/api/customers', 'admin', 201, { TaxCode: variable('customerTaxCode'), Name: 'Postman test customer' });
request('Create shared voyage', 'POST', '/api/voyages', 'admin', 201, { VesselName: 'Postman test vessel', VoyageNumber: variable('runSuffix'), Etd: variable('futureExpiry') });
request('Create shared vehicle', 'POST', '/api/vehicles', 'admin', 201, { PlateNumber: variable('vehiclePlate'), Type: 0 });
request('Create yard block', 'POST', '/api/yard/blocks', 'admin', 201, block, saveId('blockId'));
request('Expand block', 'POST', '/api/yard/blocks/{{blockId}}/expand', 'admin', 204, { NewBays: 9, NewRows: 1, NewTiers: 2 });
request('Shrink empty extension', 'POST', '/api/yard/blocks/{{blockId}}/shrink', 'admin', 204, { NewBays: 7, NewRows: 1, NewTiers: 2 });
request('Get layout and save ground slots', 'GET', '/api/yard/blocks/{{blockId}}/layout', 'admin', 200, null, `for(let n=1;n<=4;n++) { const slot=data.slots.find(s=>s.bay===2*n-1&&s.row===1&&s.tier===1); pm.expect(slot).to.exist; pm.environment.set('slotId'+n,slot.id); }`);
request('Find paged 40ft free slots', 'GET', '/api/yard/free-slots?blockId={{blockId}}&size=40&pageSize=2', 'admin', 200, null, `pm.test('40ft uses logical even bays', () => { pm.expect(data.totalCount).to.eql(3); pm.expect(data.items).to.have.length(2); data.items.forEach(s=>pm.expect(s.bay%2).to.eql(0)); });`);
for (const [key, role] of [['yard','YardPlanner'],['order','OrderClerk'],['gate','GateClerk']]) {
  request('Create ' + role, 'POST', '/api/auth/register', 'admin', 201, { UserName: variable(key+'UserName'), Email: variable(key+'UserName')+'@example.test', FirstName: 'Postman', LastName: role, Password: variable('testUserPassword'), Role: role });
}
folder('02 - Login each role');
for (const [key, name] of [['yard','YardPlanner'],['order','OrderClerk'],['gate','GateClerk']]) request(name+' login', 'POST', '/api/auth/login', null, 200, { UserName: variable(key+'UserName'), Password: variable('testUserPassword') }, token(key));
folder('03 - Delivery order quota 3');
request('OrderClerk creates quota 3', 'POST', '/api/delivery-orders', 'order', 201, order(), saveId('orderId'));
request('Read order by number', 'GET', '/api/delivery-orders/by-number/{{orderNumber}}', 'order', 200, null, `pm.test('initial quota 3',()=>pm.expect(data.items[0].remainingQuantity).to.eql(3));`);
folder('04 - Register and Gate-In four containers');
for (let n=1;n<=4;n++) {
  request('Register container '+n, 'POST', '/api/containers', 'gate', 201, registration('containerNumber'+n), saveId('containerId'+n)+`\npm.test('Location is a readable by-number route',()=>pm.expect(pm.response.headers.get('Location')).to.include('/by-number/'));`);
  request('Gate-In container '+n, 'POST', '/api/gate/in', 'gate', 201, { ContainerId: variable('containerId'+n), InVehicleNumber: variable('vehiclePlate'), SlotId: variable('slotId'+n), Classification: 1, Condition: n === 4 ? 1 : 0, VehicleType: 0 });
}
request('Assign container 1 to order', 'POST', '/api/delivery-orders/{{orderNumber}}/assign-containers', 'order', 200, { ContainerNumbers: [variable('containerNumber1')] }, `pm.environment.set('bookingId1', data.bookingIds[0]);`);
request('Read order bookings and quota', 'GET', '/api/delivery-orders/{{orderNumber}}/bookings?pageSize=1', 'order', 200, null,
  `pm.test('booking ID and remaining quota',()=>{ pm.expect(data.bookings.totalCount).to.eql(1); pm.expect(data.bookings.items[0].id).to.eql(pm.environment.get('bookingId1')); pm.expect(data.itemCounts[0].reservedQuantity).to.eql(1); pm.expect(data.itemCounts[0].availableQuantity).to.eql(2); });`);
request('Release booking for container 1', 'DELETE', '/api/delivery-orders/bookings/{{bookingId1}}', 'order', 204);
request('Re-assign container 1 to order', 'POST', '/api/delivery-orders/{{orderNumber}}/assign-containers', 'order', 200, { ContainerNumbers: [variable('containerNumber1')] });
folder('05 - YardPlanner moves and inventory');
const inventory = '/api/reports/yard-inventory?lineOperator={{operatorCode}}';
request('Inventory 4', 'GET', inventory, 'yard', 200, null, `pm.test('4 containers in depot',()=>pm.expect(data.totalContainers).to.eql(4));`);
request('Shrink with occupied bay is rejected', 'POST', '/api/yard/blocks/{{blockId}}/shrink', 'yard', 422, { NewBays: 5, NewRows: 1, NewTiers: 2 });
request('Remove first container', 'POST', '/api/yard/removals', 'yard', 204, position);
request('Removed still belongs to depot', 'GET', inventory, 'yard', 200, null, `pm.test('4 with one Unplaced',()=>{pm.expect(data.totalContainers).to.eql(4); pm.expect(data.groups.items.find(g=>g.blockId===null).containerCount).to.eql(1);});`);
request('Place first container again', 'POST', '/api/yard/placements', 'yard', 204, { ...position, ContainerId: variable('containerId1') });
request('Relocate first above second', 'POST', '/api/yard/relocations', 'yard', 204, { BlockId: variable('blockId'), SourceBay: 1, SourceRow: 1, SourceTier: 1, TargetBay: 3, TargetRow: 1, TargetTier: 2 });
request('Supporting container cannot Gate-Out under a stack', 'POST', '/api/gate/out', 'gate', 422, gateOut(2), `pm.test('stack guard',()=>pm.expect(data.code).to.eql('YardSlot.CannotRemoveWithContainersAbove'));`);
request('Register fifth for collision test only', 'POST', '/api/containers', 'gate', 201, registration('containerNumber5'), saveId('containerId5'));
request('Occupied slot rejects Gate-In', 'POST', '/api/gate/in', 'gate', 409, { ContainerId: variable('containerId5'), InVehicleNumber: variable('vehiclePlate'), SlotId: variable('slotId4') });
folder('06 - Gate-Out quota and four reports');
for (let n=1;n<=3;n++) {
  request('Gate-Out container '+n, 'POST', '/api/gate/out', 'gate', 204, gateOut(n));
  request('Remaining quota '+(3-n), 'GET', '/api/delivery-orders/{{orderId}}', 'order', 200, null, `pm.test('remaining quota ${3-n}',()=>pm.expect(data.items[0].remainingQuantity).to.eql(${3-n}));`);
}
request('Fourth rejected by quota', 'POST', '/api/gate/out', 'gate', 422, gateOut(4), `pm.test('quota rule',()=>pm.expect(data.code).to.eql('OrderRule.NoRemainingQuantity'));`);
request('Repeated Gate-Out rejected', 'POST', '/api/gate/out', 'gate', 409, gateOut(1));
request('Inventory now 1', 'GET', inventory, 'yard', 200, null, `pm.test('one remains',()=>pm.expect(data.totalContainers).to.eql(1));`);
request('Full first-container timeline', 'GET', '/api/reports/containers/{{containerNumber1}}/history', 'yard', 200, null, `pm.test('five real events',()=>{pm.expect(data.items.map(e=>e.eventType)).to.eql(['GateIn','Remove','Place','Relocate','GateOut']); data.items.forEach(e=>{pm.expect(e.source).to.eql('Movement');pm.expect(e.actorUserId).to.be.a('string');});});`);
request('Dwell includes all four visits', 'GET', '/api/reports/dwell-times?lineOperator={{operatorCode}}&includeGatedOut=true&minDays=0', 'yard', 200, null, `pm.test('four visits',()=>pm.expect(data.totalCount).to.eql(4));`);
request('Completed order absent from expiry', 'GET', '/api/reports/expiring-orders?lineOperator={{operatorCode}}&days=7', 'order', 200, null, `pm.test('no remaining order',()=>pm.expect(data.totalCount).to.eql(0));`);
folder('07 - Negative contracts, tenant and refresh');
request('400 invalid container number', 'POST', '/api/containers', 'gate', 400, { ...registration('containerNumber6'), ContainerNumber: 'INVALID' });
request('400 invalid nested enum', 'POST', '/api/delivery-orders', 'order', 400, { ...order('expiredOrderNumber'), Items: [{ Size: 999, Type: 85, Quantity: 1 }] });
request('401 without token', 'GET', '/api/containers', null, 401);
request('403 YardPlanner cannot create order', 'POST', '/api/delivery-orders', 'yard', 403, order('expiredOrderNumber'));
request('403 OrderClerk cannot see readiness', 'GET', '/health/ready', 'order', 403);
request('404 container never registered', 'GET', '/api/reports/containers/{{containerNumber6}}/history', 'yard', 404);
request('403 tenant header cannot switch depot', 'GET', '/api/containers', 'admin', 403, null, '', { headers: [{ key: 'X-Tenant-Id', value: 'DEPOT-B' }] });
request('OrderClerk creates expired order', 'POST', '/api/delivery-orders', 'order', 201, order('expiredOrderNumber','pastExpiry'), saveId('expiredOrderId'));
request('422 expired order cannot dispatch', 'POST', '/api/gate/out', 'gate', 422, gateOut(4,'expiredOrderId'), `pm.test('expiry rule',()=>pm.expect(data.code).to.eql('OrderRule.DeliveryOrderExpired'));`);
request('Mark remaining container damaged', 'POST', '/api/containers/{{containerId4}}/damage', 'yard', 204, { Condition: 1 });
request('Filter damaged inventory', 'GET', inventory+'&condition=1', 'yard', 200, null, `pm.test('one damaged container',()=>pm.expect(data.totalContainers).to.eql(1));`);
request('Refresh GateClerk tokens', 'POST', '/api/auth/refresh', null, 200, { UserName: variable('gateUserName'), RefreshToken: variable('gateRefreshToken') }, `pm.environment.set('previousGateRefreshToken',pm.environment.get('gateRefreshToken'));\n`+token('gate'));
request('Refresh replay rejected', 'POST', '/api/auth/refresh', null, 401, { UserName: variable('gateUserName'), RefreshToken: variable('previousGateRefreshToken') });
request('New access token works', 'GET', '/api/containers/by-number/{{containerNumber4}}', 'gate', 200);

folder('08 - Forty-foot footprint and reentry');
request('Create 40ft order', 'POST', '/api/delivery-orders', 'order', 201, { ...order('fortyOrderNumber'), Items: [{ Size: 40, Type: 85, Quantity: 1 }] }, saveId('fortyOrderId'));
request('Register 40ft container', 'POST', '/api/containers', 'gate', 201, { ...registration('containerNumber6'), Size: 40, IsoCode: '42G1' }, saveId('containerId6'));
request('Gate-In 40ft through lower physical slot', 'POST', '/api/gate/in', 'gate', 201, { ContainerId: variable('containerId6'), InVehicleNumber: variable('vehiclePlate'), SlotId: variable('slotId1') });
request('Inventory counts 40ft once', 'GET', inventory+'&size=40', 'yard', 200, null, `pm.test('40ft counted once',()=>pm.expect(data.totalContainers).to.eql(1));`);
request('Remove 40ft at even bay 2', 'POST', '/api/yard/removals', 'yard', 204, { ...position, Bay: 2 });
request('Place 40ft at even bay 2', 'POST', '/api/yard/placements', 'yard', 204, { ...position, Bay: 2, ContainerId: variable('containerId6') });
request('Relocate 40ft to even bay 4', 'POST', '/api/yard/relocations', 'yard', 204, { BlockId: variable('blockId'), SourceBay: 2, SourceRow: 1, SourceTier: 1, TargetBay: 4, TargetRow: 1, TargetTier: 1 });
request('40ft occupies two slots with same ContainerId', 'GET', '/api/yard/blocks/{{blockId}}/layout', 'yard', 200, null, `pm.test('two slots one footprint',()=>{const occupied=data.slots.filter(s=>s.containerId===pm.environment.get('containerId6'));pm.expect(occupied.map(s=>s.bay).sort()).to.eql([3,5]);});`);
request('Gate-Out 40ft', 'POST', '/api/gate/out', 'gate', 204, gateOut(6,'fortyOrderId'));
request('40ft timeline has one event per operation', 'GET', '/api/reports/containers/{{containerNumber6}}/history', 'yard', 200, null, `pm.test('five logical events',()=>{pm.expect(data.totalCount).to.eql(5);pm.expect(data.items[0].toPosition.bay).to.eql(2);pm.expect(data.items[3].toPosition.bay).to.eql(4);});`);
request('Create quota 2 for reentry', 'POST', '/api/delivery-orders', 'order', 201, { ...order('reentryOrderNumber'), Items: [{ Size: 20, Type: 85, Quantity: 2 }] }, saveId('reentryOrderId'));
for (let n=1;n<=2;n++) request('Reenter old container '+n, 'POST', '/api/gate/in', 'gate', 201, { ContainerId: variable('containerId'+n), InVehicleNumber: variable('vehiclePlate'), SlotId: variable('slotId'+n) });
request('Even bay cannot remove two separate 20ft containers', 'POST', '/api/yard/removals', 'yard', 409, { ...position, Bay: 2 }, `pm.test('footprint guard',()=>pm.expect(data.code).to.eql('YardSlot.InvalidFootprint'));`);
request('Even bay cannot relocate two separate 20ft containers', 'POST', '/api/yard/relocations', 'yard', 409, { BlockId: variable('blockId'), SourceBay: 2, SourceRow: 1, SourceTier: 1, TargetBay: 4, TargetRow: 1, TargetTier: 1 });
request('Failed footprint operations preserve both containers', 'GET', '/api/yard/blocks/{{blockId}}/layout', 'yard', 200, null, `pm.test('both positions preserved',()=>{for(let n=1;n<=2;n++)pm.expect(data.slots.find(s=>s.bay===2*n-1&&s.tier===1).containerId).to.eql(pm.environment.get('containerId'+n));});`);
for (let n=1;n<=2;n++) {
  request('Gate-Out reentry '+n, 'POST', '/api/gate/out', 'gate', 204, gateOut(n,'reentryOrderId'));
  request('Reentry remaining '+(2-n), 'GET', '/api/delivery-orders/{{reentryOrderId}}', 'order', 200, null, `pm.test('reentry quota ${2-n}',()=>pm.expect(data.items[0].remainingQuantity).to.eql(${2-n}));`);
}
request('Old timeline retained after reentry', 'GET', '/api/reports/containers/{{containerNumber1}}/history', 'yard', 200, null, `pm.test('two visits without duplicate history',()=>{pm.expect(data.totalCount).to.eql(7);pm.expect(new Set(data.items.map(e=>e.lifecycleId)).size).to.eql(2);});`);
request('Final inventory still one damaged 20ft', 'GET', inventory, 'yard', 200, null, `pm.test('only fourth container remains',()=>pm.expect(data.totalContainers).to.eql(1));`);

const collection = { info: { name: 'SnpDepotManagement - Week 4', description: 'Local test data only. Run folders in order. Every run creates a unique suffix; shared catalog writes require Admin. Never export real passwords/tokens. Collection format v2.1 for file-based portability.', schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json' }, item: folders };
const values = [
  { key: 'baseUrl', value: 'http://localhost:8080', type: 'default', enabled: true },
  { key: 'adminUserName', value: 'admin', type: 'default', enabled: true },
  ...['adminPassword','testUserPassword',...['admin','yard','order','gate'].flatMap(r=>[r+'AccessToken',r+'RefreshToken'])]
    .map(key=>({ key, value: '', type: 'secret', enabled: true }))
];
writeFileSync(resolve(output,'SnpDepotManagement.postman_collection.json'), JSON.stringify(collection,null,2)+'\n');
writeFileSync(resolve(output,'SnpDepotManagement.local.postman_environment.json'), JSON.stringify({ name: 'SnpDepotManagement - LOCAL placeholders', values, _postman_variable_scope: 'environment' },null,2)+'\n');
console.log(`Generated ${folders.reduce((sum,f)=>sum+f.item.length,0)} requests with per-role tokens and no real secrets.`);
