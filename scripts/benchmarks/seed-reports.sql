-- Only a disposable Testcontainers database, never an application/production DB.
-- Caller must SET snp.allow_report_benchmark = 'true' on this connection first.
BEGIN;
DO $$ BEGIN
  IF current_database() <> 'snp_api_test' OR current_setting('snp.allow_report_benchmark', true) IS DISTINCT FROM 'true' THEN
    RAISE EXCEPTION 'Report seed requires explicitly enabled disposable snp_api_test database';
  END IF;
  IF EXISTS (SELECT 1 FROM containers WHERE owner_code IN ('BCH','ALT')) THEN
    RAISE EXCEPTION 'Benchmark seed already exists';
  END IF;
END $$;

CREATE TEMP TABLE report_seed AS
SELECT n, md5('bench-container-'||n)::uuid AS container_id,
       md5('bench-lifecycle-'||n)::uuid AS lifecycle_id,
       md5('bench-slot-'||n)::uuid AS slot_id,
       CASE WHEN n <= 10000 THEN 'DEPOT-A' ELSE 'DEPOT-B' END AS tenant,
       md5(CASE WHEN n <= 10000 THEN 'bench-block-a' ELSE 'bench-block-b' END)::uuid AS block_id,
       CASE WHEN n % 10 = 1 THEN 'BCH' ELSE 'ALT' END AS owner,
       'MSKU'||lpad((600000+n)::text,6,'0') AS raw_number
FROM generate_series(1,20000) AS n;

INSERT INTO containers(id,tenant_id,container_number,iso_code,size,type,tare_weight,max_gross_weight,owner_code,condition,classification,created_at)
SELECT container_id,tenant,raw_number||(((424 + (
  SELECT sum(substr(raw_number,p,1)::integer * (1 << (p-1))) FROM generate_series(5,10) AS p
))::integer % 11) % 10)::text,
CASE WHEN n%2=0 THEN '42G1' ELSE '22G1' END,
CASE WHEN n%2=0 THEN 'Ft40' ELSE 'Ft20' END,'Dry',2000,30000,owner,'Normal','A',now()
FROM report_seed;

INSERT INTO yard_blocks(id,tenant_id,code,name,block_type,is_virtual,max_bays,max_rows,max_tiers,created_at)
SELECT DISTINCT block_id,tenant,CASE WHEN tenant='DEPOT-A' THEN 'BENCH-A' ELSE 'BENCH-B' END,'Benchmark','Standard',false,3,20000,1,now() FROM report_seed;

INSERT INTO yard_slots(id,tenant_id,block_id,position_block,position_bay,position_row,position_tier,container_id,is_occupied)
SELECT slot_id,tenant,block_id,CASE WHEN tenant='DEPOT-A' THEN 'BENCH-A' ELSE 'BENCH-B' END,1,n,1,container_id,true
FROM report_seed WHERE n%5<>0 AND n%4<>0
UNION ALL
SELECT md5('bench-slot-upper-'||n)::uuid,tenant,block_id,CASE WHEN tenant='DEPOT-A' THEN 'BENCH-A' ELSE 'BENCH-B' END,3,n,1,container_id,true
FROM report_seed WHERE n%2=0 AND n%5<>0 AND n%4<>0;

INSERT INTO container_lifecycles(id,tenant_id,container_id,line_operator,classification,condition,status,in_vehicle_number,in_vehicle_type,in_date,
  out_vehicle_number,out_vehicle_type,out_date,current_slot_id,movement_history_version,created_at)
SELECT lifecycle_id,tenant,container_id,owner,'A','Normal',CASE WHEN n%4=0 THEN 'GatedOut' ELSE 'InYard' END,'BENCH-IN','Truck',now()-(n%60+1)*interval '1 day',
  CASE WHEN n%4=0 THEN 'BENCH-OUT' END,CASE WHEN n%4=0 THEN 'Truck' END,CASE WHEN n%4=0 THEN now()-interval '1 day' END,
  CASE WHEN n%5<>0 AND n%4<>0 THEN slot_id END,CASE WHEN n%3=0 THEN 0 ELSE 1 END,now()
FROM report_seed;

INSERT INTO delivery_orders(id,tenant_id,order_number,line_operator,customer_tax_code,customer_name,expiry_date,outbound_vessel,is_completed,created_at)
SELECT md5('bench-order-'||n)::uuid,tenant,'BENCH-'||n,owner,'0101234567','Benchmark customer',date_trunc('day',now())+(n%18-3)*interval '1 day'+interval '12 hours','BENCH-VESSEL',false,now()
FROM report_seed WHERE n%10=1;
INSERT INTO delivery_order_items(id,delivery_order_id,size,type,required_quantity,delivered_quantity)
SELECT md5('bench-item-'||n)::uuid,md5('bench-order-'||n)::uuid,'Ft20','Dry',10,n%11 FROM report_seed WHERE n%10=1;

INSERT INTO container_movements(id,tenant_id,container_id,container_lifecycle_id,event_type,occurred_at_utc,
 to_position_block_id,to_position_block_code,to_position_bay,to_position_row,to_position_tier,vehicle_number,vehicle_type)
SELECT md5('bench-in-'||n)::uuid,tenant,container_id,lifecycle_id,'GateIn',now()-(n%60+1)*interval '1 day',
 block_id,CASE WHEN tenant='DEPOT-A' THEN 'BENCH-A' ELSE 'BENCH-B' END,CASE WHEN n%2=0 THEN 2 ELSE 1 END,n,1,'BENCH-IN','Truck'
FROM report_seed WHERE n%3<>0;
INSERT INTO container_movements(id,tenant_id,container_id,container_lifecycle_id,event_type,occurred_at_utc,
 from_position_block_id,from_position_block_code,from_position_bay,from_position_row,from_position_tier,vehicle_number,vehicle_type,delivery_order_id)
SELECT md5('bench-out-'||n)::uuid,tenant,container_id,lifecycle_id,'GateOut',now()-interval '1 day',
 block_id,CASE WHEN tenant='DEPOT-A' THEN 'BENCH-A' ELSE 'BENCH-B' END,CASE WHEN n%2=0 THEN 2 ELSE 1 END,n,1,'BENCH-OUT','Truck',md5('bench-dispatch-'||n)::uuid
FROM report_seed WHERE n%3<>0 AND n%4=0;
INSERT INTO container_movements(id,tenant_id,container_id,container_lifecycle_id,event_type,occurred_at_utc,
 from_position_block_id,from_position_block_code,from_position_bay,from_position_row,from_position_tier)
SELECT md5('bench-remove-'||n)::uuid,tenant,container_id,lifecycle_id,'Remove',now()-interval '1 hour',
 block_id,CASE WHEN tenant='DEPOT-A' THEN 'BENCH-A' ELSE 'BENCH-B' END,CASE WHEN n%2=0 THEN 2 ELSE 1 END,n,1
FROM report_seed WHERE n%5=0 AND n%4<>0;
COMMIT;
ANALYZE containers;
ANALYZE container_lifecycles;
ANALYZE container_movements;
ANALYZE yard_slots;
ANALYZE yard_blocks;
ANALYZE delivery_orders;
ANALYZE delivery_order_items;
