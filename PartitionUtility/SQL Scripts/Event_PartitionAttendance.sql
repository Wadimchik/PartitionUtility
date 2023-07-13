DROP EVENT IF EXISTS schema_name.table_name_partition_attendance_event_partitionSize_measure;

CREATE EVENT schema_name.table_name_partition_attendance_event_partitionSize_measure
	ON SCHEDULE
    EVERY partitionSize measure
    STARTS now()
DO
BEGIN
	DECLARE target_shema varchar(64);
	DECLARE target_table varchar(64);
    DECLARE table_max_size_GB DOUBLE;
	DECLARE table_size_status varchar(64);
    DECLARE datetime_start datetime(3);
        
    SET target_shema = 'schema_name';
	SET target_table = 'table_name';
    SET table_max_size_GB = tableMaxSize;
    SET datetime_start = curtime(3);
    SET table_size_status = 'Ошибка';
    
   SET @target_shema = target_shema;   
   SET @target_table = target_table;  
   SET @q = 'SELECT round(((data_length + index_length) / 1024 / 1024 / 1024), 2) FROM information_schema.TABLES WHERE table_schema = ? AND TABLE_NAME = ? INTO @table_size_gb';
   PREPARE st FROM @q;
   EXECUTE st USING  @target_shema, @target_table;
   DEALLOCATE PREPARE st;
   
	IF @table_size_gb <= table_max_size_GB THEN
		SET table_size_status = 'Ок';
		SELECT CONCAT(@table_size_gb, ' GB') INTO @formatted_table_size_gb;
		SELECT CONCAT('Проверка размера таблицы ', @target_shema, '.', @target_table) INTO @formatted_event_message;
        Insert into  schema_name.partition_log (time, status, action, message, duration) 
			values (datetime_start, table_size_status, @formatted_event_message, @formatted_table_size_gb, CONCAT(SEC_TO_TIME(TIMESTAMPDIFF(SECOND, datetime_start, curtime(3))), ' sec'));
    END IF;
    
   IF @table_size_gb > table_max_size_GB THEN
	   SET table_size_status = 'Предупреждение';
       SELECT CONCAT('Проверка размера таблицы ', @target_shema, '.', @target_table) INTO @formatted_event_message;
	   SELECT CONCAT('Превышение размера таблицы. Размер таблицы ', @table_size_gb, ' GB') INTO @formatted_message;
	  Insert into  schema_name.partition_log (time, status, action, message, duration) 
		values (datetime_start, table_size_status, @formatted_event_message, @formatted_message, CONCAT(SEC_TO_TIME(TIMESTAMPDIFF(SECOND, datetime_start, curtime(3))), ' sec'));
        
       SET @target_shema = target_shema;
	   SET @target_table = target_table;
       SET @p_future_name = 'p_future';
	   SET @q = 'SELECT PARTITION_NAME FROM information_schema.partitions WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ? AND PARTITION_NAME IS NOT NULL AND PARTITION_NAME NOT LIKE ? 
			order by PARTITION_NAME ASC LIMIT 1 INTO @last_partition_name';
	   PREPARE st FROM @q;
	   EXECUTE st USING  @target_shema, @target_table, @p_future_name;
	   DEALLOCATE PREPARE st;
            
	   SET @partition = @last_partition_name;
	   SET @q = 'SELECT CONCAT(''ALTER TABLE '', @target_shema, ''.'', @target_table, '' DROP PARTITION '', @partition) INTO @query';
	   PREPARE st FROM @q;
	   EXECUTE st;
	   DEALLOCATE PREPARE st;
            		
      PREPARE st FROM @query;
      EXECUTE st;
      DEALLOCATE PREPARE st;
      
	   SET @target_shema = target_shema;
	   SET @target_table = target_table;
	   SET @q = 'SELECT round(((data_length + index_length) / 1024 / 1024 / 1024), 2) FROM information_schema.TABLES WHERE table_schema = ? AND TABLE_NAME = ? INTO @table_size_gb';
	   PREPARE st FROM @q;
	   EXECUTE st USING  @target_shema, @target_table;
	   DEALLOCATE PREPARE st;
       
   	   SELECT CONCAT('Очистка последней партиции ', @target_shema, '.', @target_table, ' ', @last_partition_name) INTO @formatted_event_message;
      SELECT CONCAT('Размер таблицы ', @table_size_gb, ' GB') INTO @formatted_message;
	  Insert into  schema_name.partition_log (time, status, action, message, duration) 
		values (datetime_start, table_size_status, @formatted_event_message, @formatted_message, CONCAT(SEC_TO_TIME(TIMESTAMPDIFF(SECOND, datetime_start, curtime(3))), ' sec'));
    END IF;
       
      do SLEEP(1);
       SET datetime_start = curtime(3);
       
       SELECT CONCAT('Начато секционирование (sectorSize days) ', @target_shema, '.', @target_table) INTO @formatted_event_action_debug;
	   Insert into  schema_name.partition_log (time, status, action, message, duration) 
	   values (datetime_start, table_size_status, @formatted_event_action_debug, @formatted_table_size_gb, CONCAT(SEC_TO_TIME(TIMESTAMPDIFF(SECOND, datetime_start, curtime(3))), ' sec'));
	

    CALL schema_name.table_name_drop_old_partitions_sectorSize_days(@target_shema, @target_table, sectorCount);
	CALL schema_name.table_name_create_new_partitions_sectorSize_days(@target_shema, @target_table, 3);
    
        SET datetime_start = curtime(3);
        SELECT CONCAT('Завершено секционирование (sectorSize days) ', @target_shema, '.', @target_table) INTO @formatted_event_action;
    SELECT CONCAT(@target_shema, '.', @target_table) INTO @formatted_event_message;
		
    Insert into  schema_name.partition_log (time, status, action, message, duration) 
		values (datetime_start, 'Ок', @formatted_event_action, @formatted_event_message, CONCAT(SEC_TO_TIME(TIMESTAMPDIFF(SECOND, datetime_start, curtime(3))), ' sec'));		
END;