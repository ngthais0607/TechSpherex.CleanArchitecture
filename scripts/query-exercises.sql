\set ON_ERROR_STOP on
\set tenant 'DEPOT-A'
\pset pager off

-- Script chỉ đọc, chạy được nhiều lần. Tenant variable được psql quote bằng :'tenant'.
BEGIN READ ONLY;

-- 1. Kết nối và marker seed.
SELECT current_database(), current_user, now();
SELECT tenant_id, dataset_version, completed_at_utc
FROM demo_seed_runs WHERE tenant_id = :'tenant';

-- 2. SELECT / WHERE / ORDER BY / LIMIT.
SELECT container_number, size, type, owner_code
FROM containers WHERE tenant_id = :'tenant'
ORDER BY container_number LIMIT 10;

-- 3. GROUP BY và TEU của tồn bãi.
SELECT c.size, c.type, count(*) AS containers,
       sum(CASE WHEN c.size = 'Ft40' THEN 2 ELSE 1 END) AS teu
FROM container_lifecycles lc
JOIN containers c ON c.id = lc.container_id AND c.tenant_id = lc.tenant_id
WHERE lc.tenant_id = :'tenant' AND lc.status = 'InYard'
GROUP BY c.size, c.type ORDER BY c.size, c.type;

-- 4. LEFT JOIN: vị trí thật hoặc block ảo. 40ft hiển thị bay chẵn giữa hai slot lẻ.
SELECT c.container_number, c.size, lc.status,
       coalesce(b.code, vb.code) AS block,
       CASE WHEN s.id IS NOT NULL THEN s.position_bay + CASE WHEN c.size = 'Ft40' THEN 1 ELSE 0 END END AS bay,
       s.position_row AS row, s.position_tier AS tier,
       lc.in_date AT TIME ZONE 'Asia/Ho_Chi_Minh' AS in_time_vn
FROM container_lifecycles lc
JOIN containers c ON c.id = lc.container_id AND c.tenant_id = lc.tenant_id
LEFT JOIN yard_slots s ON s.id = lc.current_slot_id AND s.tenant_id = lc.tenant_id
LEFT JOIN yard_blocks b ON b.id = s.block_id AND b.tenant_id = lc.tenant_id
LEFT JOIN yard_blocks vb ON vb.id = lc.virtual_block_id AND vb.tenant_id = lc.tenant_id
WHERE lc.tenant_id = :'tenant' AND lc.status = 'InYard'
ORDER BY c.container_number;

-- 5. Quota lệnh; RemainingQuantity là thuộc tính tính toán, không phải cột DB.
SELECT o.order_number, o.line_operator, o.expiry_date, o.is_completed,
       i.size, i.type, i.required_quantity, i.delivered_quantity,
       i.required_quantity - i.delivered_quantity AS remaining_to_deliver
FROM delivery_orders o JOIN delivery_order_items i ON i.delivery_order_id = o.id
WHERE o.tenant_id = :'tenant' ORDER BY o.order_number, i.size, i.type;

-- 6. Booking lưu enum số. Không suy ra hiệu lực lệnh chỉ từ status Reserved.
SELECT o.order_number, c.container_number,
       CASE bk.status WHEN 1 THEN 'Reserved' WHEN 2 THEN 'Shipped'
            WHEN 3 THEN 'Cancelled' WHEN 4 THEN 'Expired' ELSE 'Unknown' END AS booking_status,
       o.expiry_date >= now() AS order_not_expired, bk.reserved_at, bk.shipped_at
FROM delivery_order_bookings bk
JOIN delivery_orders o ON o.id = bk.order_id AND o.tenant_id = bk.tenant_id
JOIN containers c ON c.id = bk.container_id AND c.tenant_id = bk.tenant_id
WHERE bk.tenant_id = :'tenant' ORDER BY o.order_number, c.container_number;

-- 7. Timeline 20 sự kiện gần nhất. Có thể thêm AND c.container_number = '...'.
SELECT c.container_number, m.event_type,
       m.occurred_at_utc AT TIME ZONE 'Asia/Ho_Chi_Minh' AS event_time_vn
FROM container_movements m
JOIN containers c ON c.id = m.container_id AND c.tenant_id = m.tenant_id
WHERE m.tenant_id = :'tenant' ORDER BY m.occurred_at_utc DESC, m.id LIMIT 20;

-- 8. CTE: số nền trước thao tác là 20 InYard, 3 GatedOut, 27 NeverIn.
WITH states AS (
    SELECT c.id, CASE
        WHEN EXISTS (SELECT 1 FROM container_lifecycles lc WHERE lc.container_id=c.id AND lc.tenant_id=c.tenant_id AND lc.status='InYard') THEN 'InYard'
        WHEN EXISTS (SELECT 1 FROM container_lifecycles lc WHERE lc.container_id=c.id AND lc.tenant_id=c.tenant_id AND lc.status='GatedOut') THEN 'GatedOut'
        ELSE 'NeverIn' END AS state
    FROM containers c WHERE c.tenant_id = :'tenant'
)
SELECT state, count(*) AS count FROM states GROUP BY state ORDER BY state;

-- 9. EXPLAIN ANALYZE thực thi SELECT; DB 50 dòng có thể chọn Seq Scan hợp lý.
EXPLAIN (ANALYZE, BUFFERS)
SELECT container_number FROM containers WHERE tenant_id = :'tenant' ORDER BY container_number LIMIT 10;

ROLLBACK;
