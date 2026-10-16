-- Upgrade only: run AddUserTenant first, then this script, then RequireUserTenant.
-- Operator provides a transaction-local mapping from an approved CSV using psql:
-- BEGIN;
-- CREATE TEMP TABLE user_tenant_mapping(user_id uuid PRIMARY KEY, tenant_id varchar(50) NOT NULL);
-- \copy user_tenant_mapping FROM '/absolute/approved-user-depots.csv' CSV HEADER
-- \i scripts/apply-user-tenants.sql
-- COMMIT;
-- Never guess a tenant for existing accounts. Supply mapping for every existing user.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM application_users u LEFT JOIN user_tenant_mapping m ON m.user_id = u.id
               WHERE m.user_id IS NULL OR length(trim(m.tenant_id)) = 0) THEN
        RAISE EXCEPTION 'Missing or empty user depot mapping; deployment must stop';
    END IF;
    IF EXISTS (SELECT 1 FROM user_tenant_mapping m LEFT JOIN application_users u ON u.id = m.user_id WHERE u.id IS NULL) THEN
        RAISE EXCEPTION 'Mapping contains an unknown user';
    END IF;
    IF EXISTS (SELECT 1 FROM application_users u JOIN user_tenant_mapping m ON m.user_id = u.id
               WHERE u.tenant_id IS NOT NULL AND u.tenant_id <> trim(m.tenant_id)) THEN
        RAISE EXCEPTION 'Mapping would change an existing depot assignment';
    END IF;
    UPDATE application_users u SET tenant_id = trim(m.tenant_id)
    FROM user_tenant_mapping m WHERE m.user_id = u.id AND u.tenant_id IS NULL;
END $$;
