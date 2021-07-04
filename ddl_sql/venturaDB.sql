CREATE TABLE ventura. (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),

	CONSTRAINT read_job_pkey PRIMARY KEY (id)

)
TABLESPACE pg_default;

CREATE TABLE ventura.system_param (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	biztype  varchar(20),
    module_name varchar(20),
	params_json json,
	start_ts_utc timestamp,
	end_ts_utc timestamp,
	CONSTRAINT system_param_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;
--
insert into ventura.system_param (biztype, module_name, params_json) 
values ('system','lite',
'{"inputdir":"c:\\zunk\\lite\\input", "output_par":"nps_lite", "output_lite":"NPSLite", "output_apy":"APY", "photo_max_per_dir":"150", "expect_max_subdir":"9999"
, "workdir":"c:\\zunk\\lite\\work", "systemdir":"c:\\users\\spadte\\source\\repos\\padtes\\DotNetCorePOC\\ddl_sql"}');


CREATE TABLE ventura.fileinfo (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	fname varchar(60),
	fpath varchar(256),
	fsize integer,
	biztype  varchar(20),
    module_name varchar(20),
	direction char(1),
	importedfrom	varchar(30),
	courier_sname	varchar(10),
	courier_mode	varchar(5),
	nprodrecords	integer,
	-- nrecords	JSON?
	archivepath	varchar(255),
	archiveafter	integer,
	purgeafter	integer,
	addeddate	TIMESTAMP,
	addedby	varchar,
	addedfromip	varchar(16),
	updatedate	TIMESTAMP,
	updatedby	varchar(30),
	updatedfromip	varchar(16),
	isdeleted	bool,
	inp_rec_status varchar(10),
	inp_rec_status_ts_utc TIMESTAMP,
	-- meta	JSON	
	CONSTRAINT fileinfo_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

CREATE TABLE ventura.filedetails
 (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	fileinfo_id INTEGER,
	-- doc_id 
	prod_id varchar(40), 
	-- docdate
	courier_id varchar(10),
	json_data jsonb,
	row_number INTEGER,
	ack_number varchar(20),
	apy_flag char(1),
	files_saved jsonb,
	CONSTRAINT filedetails_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

CREATE TABLE ventura.filetypemaster (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	isactive	bool,
	biztype  varchar(20),
    module_name varchar(20),
	archiveafter 	integer,
	purgeafter 	integer,
	fname_pattern	varchar(200),
	fname_pattern_attr	varchar(200),
	fname_pattern_name	varchar(200),
	ext	varchar(5),
	ftype	varchar(20),
	file_def_json json,
	file_def_json_fName varchar(50),
	CONSTRAINT filetypemaster_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName)
values('1','lite_inp','lite','lite_input.json');

CREATE TABLE ventura.counters(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	isactive	bool,
-- period	
	counter_name	varchar(50),
	descript varchar(200),
	parent_id	integer, -- 0 : master
	is_immutable	boolean,
	archived	boolean,
	pat	  varchar(100),
	start_num	integer,
	step	integer,
	end_num	integer,
	next_num	integer,
	autoreset	bool,
	lock_key integer,
	CONSTRAINT counters_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

/*
select * from ventura.fileinfo ;
select * from ventura.filedetails order by fileinfo_id, id;
select * from ventura.counters;
*/
-- delete from ventura.fileinfo;
-- ALTER SEQUENCE ventura.fileinfo_id_seq RESTART WITH 1;
-- delete from ventura.filedetails;
-- ALTER SEQUENCE ventura.filedetails_id_seq RESTART WITH 1;

--update ventura.counters set next_num=start_num where parent_id > 0;

-- FUNCTION: ventura.get_serial_number(character varying, character varying, boolean, integer)

-- DROP FUNCTION ventura.get_serial_number(character varying, character varying, boolean, integer);

CREATE OR REPLACE FUNCTION ventura.get_serial_number(
	master_type character varying,
	pdoc_val character varying,
	init_if_need boolean,
	lock_id integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
	SET search_path FROM CURRENT
AS $BODY$
declare 
  lser_no integer;
  lmaster_id INTEGER;
  child_id INTEGER;
begin
  lser_no := -1;
  lmaster_id := -1;
  select id 
  into lmaster_id
  from ventura.counters
  where counter_name = master_type and parent_id = 0;

  if not found then
    if init_if_need = '1' then
	    insert into ventura.counters (isactive, counter_name, descript, parent_id)
 	    values ('1', master_type, 'auto create', 0) RETURNING id into lmaster_id;

		insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key)
 	    values ('1', pdoc_val, 'auto create', lmaster_id, 1, 1, 99999, 2, lock_id);
		
 	    lser_no := 1;
    end if;
  else
    select next_num, id 
    into lser_no, child_id
    from ventura.counters
    where counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
	 and (lock_id = lock_key or lock_id <= 0)
	order by start_num limit 1;

    if found then
      lser_no := lser_no + 1;
      update ventura.counters 
	  set next_num = lser_no
      where id =child_id;
 	else
	  if init_if_need = '1' and lock_id <= 0 then
		insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key)
 	    values ('1', pdoc_val, 'auto create', lmaster_id, 1, 1, 99999, 2, lock_id);
		
 	    lser_no := 1;
	  end if;
	end if;
 end if;

  return lser_no;
 
end;
$BODY$;

ALTER FUNCTION ventura.get_serial_number(character varying, character varying, boolean, integer)
    OWNER TO postgres;

-- FUNCTION: ventura.lock_counter(character varying, character varying, integer)

-- DROP FUNCTION ventura.lock_counter(character varying, character varying, integer);

CREATE OR REPLACE FUNCTION ventura.lock_counter(
	master_type character varying,
	pdoc_val character varying,
	lock_id integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
	SET search_path FROM CURRENT
AS $BODY$
declare 
  lmaster_id INTEGER;
  child_id INTEGER;
begin
  lmaster_id := -1;
  child_id := -1;
  select id 
  into lmaster_id
  from ventura.counters
  where counter_name = master_type and parent_id = 0;

  if found then
    select id 
    into child_id
    from ventura.counters
    where counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
	 and COALESCE(lock_key,0) <= 0
	order by start_num limit 1;

    if found then
      update ventura.counters 
	  set lock_key = lock_id
      where id =child_id;
	end if;
  end if;

  return child_id;
 
end;
$BODY$;

ALTER FUNCTION ventura.lock_counter(character varying, character varying, integer)
    OWNER TO postgres;
