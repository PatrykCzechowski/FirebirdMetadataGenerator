-- Example stored procedure: calculate order total
SET TERM ^ ;

CREATE PROCEDURE SP_CALCULATE_ORDER_TOTAL
(
    ORDER_ID INTEGER
)
RETURNS
(
    TOTAL_AMOUNT NUMERIC(15, 2)
)
AS
BEGIN
    -- Simple example procedure
    SELECT SUM(TOTAL_AMOUNT)
    FROM ORDERS
    WHERE ID = :ORDER_ID
    INTO :TOTAL_AMOUNT;
    
    IF (TOTAL_AMOUNT IS NULL) THEN
        TOTAL_AMOUNT = 0.00;
    
    SUSPEND;
END^

SET TERM ; ^
