DROP PROCEDURE IF EXISTS schema_name.table_name_create_new_partitions_size_months;

CREATE PROCEDURE schema_name.table_name_create_new_partitions_size_months(p_schema varchar(64), p_table varchar(64), p_months_to_keep int)
   LANGUAGE SQL
   NOT DETERMINISTIC
   SQL SECURITY INVOKER
BEGIN  
   DECLARE done INT DEFAULT FALSE;
   DECLARE current_partition_name varchar(64);
   DECLARE current_partition_ts int;
   
   DECLARE cur1 CURSOR FOR 
   SELECT partition_name 
   FROM information_schema.partitions 
   WHERE TABLE_SCHEMA = p_schema 
   AND TABLE_NAME = p_table 
   AND PARTITION_NAME != 'p_past'
   AND PARTITION_NAME != 'p_future'
   AND PARTITION_NAME = @partition_name_to_add;
   
   DECLARE cur2 CURSOR FOR 
   SELECT partition_name, partition_range_ts 
   FROM partitions_to_add;
   
   DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
   
   DROP TEMPORARY TABLE IF EXISTS partitions_to_add;
   
   CREATE TEMPORARY TABLE partitions_to_add (
      partition_name varchar(64),
      partition_range_ts int
   );
   
   SET @partitions_added = FALSE;
   SET @months_ahead = 1;
   
   WHILE @months_ahead <= p_months_to_keep * sectorSize DO
      
      SET @date = CURDATE();
      SET @q = 'SELECT DATE_ADD(?, INTERVAL ? MONTH) INTO @month_to_add';
      PREPARE st FROM @q;
      EXECUTE st USING @date, @months_ahead;
      DEALLOCATE PREPARE st;
      
      SET @months_ahead = @months_ahead + sectorSize;
      
      SET @q = 'SELECT DATE_FORMAT(@month_to_add, ''%Y%m'') INTO @formatted_month_to_add';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
      
      SET @q = 'SELECT CONCAT(''p'', @formatted_month_to_add) INTO @partition_name_to_add';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
     
      SET done = FALSE;
      SET @first = TRUE;
     
      OPEN cur1;
      
      read_loop: LOOP
         FETCH cur1 INTO current_partition_name;
      
         IF done AND @first THEN

            SET @q = 'SELECT DATE_FORMAT(@month_to_add, ''%Y-%m-01 00:00:00'') INTO @month_to_add';
            PREPARE st FROM @q;
            EXECUTE st;
            DEALLOCATE PREPARE st; 

            SET @q = 'SELECT DATE_ADD(?, INTERVAL 1 MONTH) INTO @partition_end_date';
            PREPARE st FROM @q;
            EXECUTE st USING @month_to_add;
            DEALLOCATE PREPARE st;

            SELECT TO_DAYS(@partition_end_date) INTO @partition_end_ts;
         
            INSERT INTO partitions_to_add VALUES (@partition_name_to_add, @partition_end_ts);
            SET @partitions_added = TRUE;
         END IF;
        
         IF NOT @first THEN
            LEAVE read_loop;
         END IF;
        
         SET @first = FALSE;
      END LOOP;

     CLOSE cur1;
   END WHILE;
   
   select * from partitions_to_add;
   
   IF @partitions_added THEN
   
      SET @schema = p_schema;
      SET @table = p_table;
      SET @q = 'SELECT CONCAT(''ALTER TABLE '', @schema, ''.'', @table, '' REORGANIZE PARTITION p_future INTO ( '') INTO @query';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
     
      SET done = FALSE;
      SET @first = TRUE;
     
      OPEN cur2;
      read_loop: LOOP
         FETCH cur2 INTO current_partition_name, current_partition_ts;
       
        IF done THEN
            LEAVE read_loop;
         END IF;
      
         IF NOT @first THEN
            SET @q = 'SELECT CONCAT(@query, '', '') INTO @query';
            PREPARE st FROM @q;
            EXECUTE st;
            DEALLOCATE PREPARE st;
         END IF;

         SET @partition_name =  current_partition_name;
         SET @partition_ts =  current_partition_ts;         
         SET @q = 'SELECT CONCAT(@query, ''PARTITION '', @partition_name, '' VALUES LESS THAN ('', @partition_ts, '')'') INTO @query';
         PREPARE st FROM @q;
         EXECUTE st;
         DEALLOCATE PREPARE st;
       
         SET @first = FALSE;
      END LOOP;
      CLOSE cur2;
     
      SET @q = 'SELECT CONCAT(@query, '', PARTITION p_future VALUES LESS THAN (MAXVALUE))'') INTO @query';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
     
      PREPARE st FROM @query;
      EXECUTE st;
      DEALLOCATE PREPARE st;  
   END IF;
   
   DROP TEMPORARY TABLE partitions_to_add;
END;
