\set ON_ERROR_STOP on
-- Run on both source and target; compare the ordered results in the restore drill.
SELECT 'containers' AS entity, tenant_id, count(*) AS count FROM containers GROUP BY tenant_id
UNION ALL SELECT 'users',tenant_id,count(*) FROM application_users GROUP BY tenant_id
UNION ALL SELECT 'lifecycles',tenant_id,count(*) FROM container_lifecycles GROUP BY tenant_id
UNION ALL SELECT 'movements',tenant_id,count(*) FROM container_movements GROUP BY tenant_id
UNION ALL SELECT 'orders',tenant_id,count(*) FROM delivery_orders GROUP BY tenant_id
UNION ALL SELECT 'occupied_slots',tenant_id,count(*) FROM yard_slots WHERE is_occupied GROUP BY tenant_id
ORDER BY entity, tenant_id;
SELECT o.tenant_id,o.order_number,i.size,i.type,i.required_quantity,i.delivered_quantity,
       i.required_quantity-i.delivered_quantity AS remaining
FROM delivery_order_items i JOIN delivery_orders o ON o.id=i.delivery_order_id
ORDER BY o.tenant_id,o.order_number,i.size,i.type,i.id;
SELECT tenant_id,container_id,container_lifecycle_id,id,event_type,occurred_at_utc
FROM container_movements ORDER BY tenant_id,container_id,occurred_at_utc,id;
