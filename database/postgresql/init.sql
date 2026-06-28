-- EnterprisePlatform PostgreSQL 一键初始化（建表 + 种子数据）
-- 用法：psql -U platform -d enterprise -f init.sql

\ir 01_schema.sql
\ir 02_seed.sql
