-- Prepara um PostgreSQL instalado na máquina para desenvolvimento da Clavus API.
-- Executar como superusuário (ex.: postgres):
--   psql -U postgres -f scripts/criar-usuario-dev.sql
-- Credenciais apenas para desenvolvimento.

create role clavus with login password 'clavus' createdb;

create database clavus owner clavus;
