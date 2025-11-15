-- Example domain: email type
CREATE DOMAIN D_EMAIL AS VARCHAR(255) CHECK (VALUE LIKE '%@%');
