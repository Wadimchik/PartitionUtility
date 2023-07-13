DROP PROCEDURE IF EXISTS schema_name.table_name_drop_old_partitions_size_days;

CREATE PROCEDURE schema_name.table_name_drop_old_partitions_size_days(p_schema varchar(64), p_table varchar(64), p_amount_of_partitions_to_keep int)
   LANGUAGE SQL
   NOT DETERMINISTIC
   SQL SECURITY INVOKER
BEGIN  
   DECLARE done INT DEFAULT FALSE;
   DECLARE current_partition_name varchar(64);
   DECLARE drop_part_count int default 0;
   
   DECLARE cur1 CURSOR FOR
	   SELECT partition_name
	   FROM information_schema.partitions
	   WHERE TABLE_SCHEMA = p_schema
	   AND TABLE_NAME = p_table
	   AND PARTITION_NAME != 'p_future'
	   AND PARTITION_NAME < @last_partition_name_to_keep;
   
   DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
     
   SET @date = CURDATE();
   SET @days_to_keep = p_amount_of_partitions_to_keep * sectorSize;   
   SET @q = 'SELECT DATE_SUB(?, INTERVAL ? DAY) INTO @last_day_to_keep';
   PREPARE st FROM @q;
   EXECUTE st USING @date, @days_to_keep;
   DEALLOCATE PREPARE st;
   
   SET @q = 'SELECT DATE_FORMAT(@last_day_to_keep, ''%Y%m%d'') INTO @formatted_last_day_to_keep';
   PREPARE st FROM @q;
   EXECUTE st;
   DEALLOCATE PREPARE st;

   SET @q = 'SELECT CONCAT(''p'', @formatted_last_day_to_keep) INTO @last_partition_name_to_keep';
   PREPARE st FROM @q;
   EXECUTE st;
   DEALLOCATE PREPARE st;
   
   SELECT CONCAT('Dropping all partitions before: ', @last_partition_name_to_keep);
   
   SET @first = TRUE;
   
   OPEN cur1;
   read_loop: LOOP
      FETCH cur1 INTO current_partition_name;
   
      IF done THEN
         LEAVE read_loop;
      END IF;
     
      SELECT CONCAT('Drop partition: ', current_partition_name);
   
      SET @schema = p_schema;
      SET @table = p_table;
      SET @partition = current_partition_name;
      SET @q = 'SELECT CONCAT(''ALTER TABLE '', @schema, ''.'', @table, '' DROP PARTITION '', @partition) INTO @query';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
      
      PREPARE st FROM @query;
      EXECUTE st;
      DEALLOCATE PREPARE st;
		
      SET @first = FALSE;
   END LOOP;
   
   select FOUND_ROWS() into @drop_part_count;
   CLOSE cur1;
   
   SELECT CONCAT('Drop done. Count: ', @drop_part_count); 
   SELECT CONCAT('Procedure Done. "drop_old_partitions".');
END;
