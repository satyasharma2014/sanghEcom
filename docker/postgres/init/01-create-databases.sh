#!/bin/bash
# ============================================================================
# File: docker/postgres/init/01-create-databases.sh
# Postgres container ke pehli baar start hone par chalti hai (volume khali ho).
# Har service ko apna USER aur apna DATABASE milta hai.
# Dusre service ka DB dekh/badal nahi sakte (least privilege).
# ============================================================================

# -e: koi command fail ho to script ruk jaye
# -u: undefined variable par error
# -o pipefail: pipe mein koi part fail ho to error
set -euo pipefail

# Helper function: user + database banata hai
#   $1 = database naam
#   $2 = user naam (aur owner)
#   $3 = password
create_service_database() {
  local db_name="$1"
  local db_user="$2"
  local db_password="$3"

  echo "Creating database '${db_name}' owned by '${db_user}'"

  # $POSTGRES_USER = superuser (docker-compose.yml se aata hai)
  # ON_ERROR_STOP=1 : SQL error par script ruk jaye, chhupe nahi
  # --dbname postgres : system DB se connect, CREATE DATABASE ke liye
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<EOSQL
CREATE USER ${db_user} WITH PASSWORD '${db_password}';
CREATE DATABASE ${db_name} OWNER ${db_user};
EOSQL
}

create_service_database "catalog_db"      "catalog"      "$CATALOG_DB_PASSWORD"
create_service_database "inventory_db"    "inventory"    "$INVENTORY_DB_PASSWORD"
create_service_database "payment_db"      "payment"      "$PAYMENT_DB_PASSWORD"
create_service_database "notification_db" "notification" "$NOTIFICATION_DB_PASSWORD"
create_service_database "orders_db"       "orders"       "$ORDERS_DB_PASSWORD"
