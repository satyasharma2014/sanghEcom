-- Har service ka user banata/update karta hai aur database ka owner banata hai.
-- Passwords psql variables se aate hain (catalog_pw, inventory_pw, ...), file mein nahi.
\set ON_ERROR_STOP on

SELECT format('CREATE ROLE %I LOGIN', 'catalog') WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'catalog')\gexec
SELECT format('ALTER ROLE %I WITH LOGIN PASSWORD %L', 'catalog', :'catalog_pw')\gexec
ALTER DATABASE catalog_db OWNER TO catalog;

SELECT format('CREATE ROLE %I LOGIN', 'inventory') WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'inventory')\gexec
SELECT format('ALTER ROLE %I WITH LOGIN PASSWORD %L', 'inventory', :'inventory_pw')\gexec
ALTER DATABASE inventory_db OWNER TO inventory;

SELECT format('CREATE ROLE %I LOGIN', 'payment') WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'payment')\gexec
SELECT format('ALTER ROLE %I WITH LOGIN PASSWORD %L', 'payment', :'payment_pw')\gexec
ALTER DATABASE payment_db OWNER TO payment;

SELECT format('CREATE ROLE %I LOGIN', 'notification') WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notification')\gexec
SELECT format('ALTER ROLE %I WITH LOGIN PASSWORD %L', 'notification', :'notification_pw')\gexec
ALTER DATABASE notification_db OWNER TO notification;

SELECT format('CREATE ROLE %I LOGIN', 'orders') WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'orders')\gexec
SELECT format('ALTER ROLE %I WITH LOGIN PASSWORD %L', 'orders', :'orders_pw')\gexec
ALTER DATABASE orders_db OWNER TO orders;
