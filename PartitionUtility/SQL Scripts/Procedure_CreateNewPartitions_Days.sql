DROP PROCEDURE IF EXISTS schema_name.table_name_create_new_partitions_size_days;

CREATE PROCEDURE schema_name.table_name_create_new_partitions_size_days(p_schema varchar(64), p_table varchar(64), p_two_days_to_keep int)
   LANGUAGE SQL
   NOT DETERMINISTIC
   SQL SECURITY INVOKER
BEGIN  
   DECLARE partition_interval int default sectorSize;
   
   DECLARE done INT DEFAULT FALSE;
   DECLARE current_partition_name_cur1 varchar(64);
   DECLARE current_partition_ts_cur2 int;
   
   	DECLARE partitionsCur_partition_name varchar(64);
	DECLARE partitionsCur_partition_datetime DATETIME(3);
	DECLARE partitionsCur_FIRST int default 0;
	DECLARE nearest_partition_to_current varchar(64);
	DECLARE nearest_partition_to_current_datetime DATETIME(3);
	DECLARE current_datetime DATETIME(3);
	DECLARE current_partition_datetime DATETIME(3);
    
   DECLARE cur1 CURSOR FOR 
   SELECT partition_name 
   FROM information_schema.partitions 
   WHERE TABLE_SCHEMA = p_schema 
   AND TABLE_NAME = p_table 
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
   
   BLOCK1:BEGIN
	DECLARE FINISHED int default 1;
    
   DECLARE partitionsCur CURSOR FOR 
   SELECT partition_name
   FROM information_schema.partitions 
   WHERE TABLE_SCHEMA = p_schema 
   AND TABLE_NAME = p_table 
   AND PARTITION_NAME != 'p_future';
   
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET FINISHED = 0;
 
	SET current_datetime = CURDATE();
	SELECT current_datetime;

	SET partitionsCur_FIRST = true;
    
    OPEN partitionsCur;
      read_loop: LOOP
           IF FINISHED = 0 THEN
            LEAVE read_loop;
        END IF;
        
         FETCH partitionsCur INTO partitionsCur_partition_name;
         
         SET partitionsCur_partition_datetime = STR_TO_DATE(REPLACE(partitionsCur_partition_name, 'p', ''), '%Y%m%D');
         
         IF partitionsCur_partition_datetime > current_datetime AND partitionsCur_FIRST THEN
			SET nearest_partition_to_current_datetime = partitionsCur_partition_datetime;
            SET partitionsCur_FIRST = false;
            
		ELSEIF partitionsCur_partition_datetime > current_datetime AND NOT partitionsCur_FIRST THEN
			IF partitionsCur_partition_datetime < nearest_partition_to_current_datetime THEN
				SET nearest_partition_to_current_datetime = partitionsCur_partition_datetime;
			END IF;
            
         END IF;
         
		 END LOOP;
	CLOSE partitionsCur;
    
    IF nearest_partition_to_current_datetime IS NOT NULL THEN
		SET current_partition_datetime =  nearest_partition_to_current_datetime;
		SELECT 'Текущая партиция найдена', current_partition_datetime;
    ELSE
        SET current_partition_datetime = DATE_ADD(curdate(), INTERVAL partition_interval DAY);
        SELECT 'Текущая партиция не найдена', current_partition_datetime;
    END IF;
    
     END BLOCK1;

    
	SET @partitions_added = FALSE;
	SET @days_ahead = 0;
   
   WHILE @days_ahead <= p_two_days_to_keep * partition_interval DO
      
      SET @date = current_partition_datetime;
      SET @q = 'SELECT DATE_ADD(?, INTERVAL ? day) INTO @day_to_add';
      PREPARE st FROM @q;
      EXECUTE st USING @date, @days_ahead;
      DEALLOCATE PREPARE st;
      
      SET @days_ahead = @days_ahead + partition_interval;
      
      SET @q = 'SELECT DATE_FORMAT(@day_to_add, ''%Y%m%d'') INTO @formatted_day_to_add';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
      
      SET @q = 'SELECT CONCAT(''p'', @formatted_day_to_add) INTO @partition_name_to_add';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
     
      SET done = FALSE;
      SET @first = TRUE;
     
      OPEN cur1;
      
      read_loop: LOOP
         FETCH cur1 INTO current_partition_name_cur1;
      
         IF done AND @first THEN

            SET @q = 'SELECT DATE_FORMAT(@day_to_add, ''%Y-%m-%d 00:00:00'') INTO @day_to_add';
            PREPARE st FROM @q;
            EXECUTE st;
            DEALLOCATE PREPARE st; 

            SET @q = 'SELECT DATE_ADD(?, INTERVAL 0 day) INTO @partition_end_date';
            PREPARE st FROM @q;
            EXECUTE st USING @day_to_add;
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
   
	  SET @schema = p_schema;
      SET @table = p_table;
      SET @q = 'SELECT CONCAT(''ALTER TABLE '', @schema, ''.'', @table, '' TRUNCATE PARTITION p_future'')';
      PREPARE st FROM @q;
      EXECUTE st;
      DEALLOCATE PREPARE st;
      
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
         FETCH cur2 INTO current_partition_name_cur1, current_partition_ts_cur2;
       
        IF done THEN
            LEAVE read_loop;
         END IF;
      
         IF NOT @first THEN
            SET @q = 'SELECT CONCAT(@query, '', '') INTO @query';
            PREPARE st FROM @q;
            EXECUTE st;
            DEALLOCATE PREPARE st;
         END IF;

         SET @partition_name =  current_partition_name_cur1;
         SET @partition_ts =  current_partition_ts_cur2;         
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
   
	SELECT CONCAT('Procedure Done. "create_new_partitions".');
END;
