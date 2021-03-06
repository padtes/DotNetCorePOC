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
, "workdir":"c:\\zunk\\lite\\work", "systemdir":"c:\\users\\spadte\\source\\repos\\padtes\\DotNetCorePOC\\ddl_sql"
, "printer_code3":"XYZ", "printer_code2":"51", "courier_awb_kvcsv":"PST=SPD"
, "printed_ok_code":"PTD"}'
);


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
	addedby	varchar(30),
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
	tx_id varchar(50),
	-- docdate
	courier_id varchar(10),
	json_data jsonb,
	row_number INTEGER,
	ack_number varchar(20),
	apy_flag char(1),
	files_saved jsonb,
	det_err_csv varchar(20),
	print_dt TIMESTAMP,
	pickup_dt TIMESTAMP,
	CONSTRAINT filedetails_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

-- alter table ventura.filedetails add tx_id varchar(50)

CREATE TABLE ventura.filedetail_actions
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    filedet_id integer,
	action_done varchar(20),
	action_void bool,
	addeddate TIMESTAMP,
	addedby varchar(30),
	voiddate TIMESTAMP,
	voidby varchar(30),
    CONSTRAINT filedetail_actions_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE ventura.filedetail_actions
    OWNER to postgres;
	
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
insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_resp','lite','lite_imm_resp.json','PRN{{sys_param(printer_code)}}RES{{now_ddmmyy}}{{serial_no}}.txt');
insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_stat','lite','lite_status_rep.json','PRN{{sys_param(printer_code)}}STS{{now_ddmmyy}}{{serial_no}}.txt');

insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_ptc_apy','lite','NPSLiteAPY_PTC.json','PRAN{{sys_param(printer_code)}}PTC{{now_ddmmyy}}{{serial_no}}.txt');
insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_ptc_nps','lite','NPSLiteAPY_PTC.json','PRAN{{sys_param(printer_code)}}PTC{{now_ddmmyy}}{{serial_no}}.txt');

insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_awb_apy','lite','NPSLiteAPY_AWB.json','PRN{{sys_param(awb_trans)}}RES{{now_ddmmyy}}{{serial_no}}.txt');
insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_awb_nps','lite','NPSLiteAPY_AWB.json','PRN{{sys_param(awb_trans)}}RES{{now_ddmmyy}}{{serial_no}}.txt');

insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_word_apy','lite','apy_letter.json','apy_{{yyyymmdd}}_{{courier_cd}}_{{serial_no}}.docx');
insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_word_nps','lite','NPS_Lite_letter.json','npsLite_{{yyyymmdd}}_{{courier_cd}}_{{serial_no}}.docx');

insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_card_apy','lite','apy_card.json','apy_{{yyyymmdd}}_{{courier_cd}}_{{serial_no}}.csv');
insert into ventura.filetypemaster(isactive,biztype,module_name,file_def_json_fName, fname_pattern)
values('1','lite_card_nps','lite','NPS_Lite_card.json','npsLite_{{yyyymmdd}}_{{courier_cd}}_{{serial_no}}.csv');

CREATE TABLE ventura.counters(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	isactive	bool,
    freq_period	 varchar(20),  -- period is reserved word. This is daily / global / date etc
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
	card_type  varchar(4), --APY - Lite - Reg
	addeddate	timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
	sort_order integer DEFAULT 1,
	CONSTRAINT counters_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

/*
alter table ventura.counters add sort_order integer DEFAULT 1;

update ventura.counters set sort_order = 1 where sort_order is null;
*/

/*
isactive	counter_name	descript	parent_id	is_immutable	archived	pat	start_num	step	end_num	next_num	autoreset	lock_key	addeddate	freq_period	card_type
TRUE	couriers_global	master rec for couriers	0	TRUE	NULL	EA{sequence}{chk_val}IN	NULL	1	NULL	NULL	NULL	NULL	7/10/21 11:00 PM	NULL	NULL
TRUE	PRF	detail rec for couriers	1	TRUE	NULL	NULL	1000	1	7000	1000	NULL	NULL	7/10/21 11:00 PM	NULL	NULL
TRUE	PSTS	sql/skp	1	NULL	NULL	NULL	30726182	1	30726271	30726259	NULL	NULL	7/10/21 11:00 PM	NULL	NULL
*/

insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num) values
('1','PSTS','sql/skp AWB range-2',1,'30739786',1,'30739840','30739786')

insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num) values
('1','PSTN','sql/skp AWB placeholder',1,'10000000',1,'99999999','10000000')

--insert into ventura.counters(isactive,counter_name,parent_id,descript) values   ('1','couriers',0, 'master rec for AWB couriers');

--insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, start_num, end_num, step) values 
--	('1','PST','AWB PST', '', '5 1', '100001', '199999', 1);

insert into ventura.counters(isactive,counter_name,parent_id) values ('1','generic',0);
--"couriers" master rec for couriers
--"detail rec for couriers"

insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, card_type) values 
	('1','couriers','daily master of all couriers-APY', 'daily', '0', 'apy');

insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, start_num, end_num, next_num, step, card_type) values 
	('1','PRF','daily master PRF', 'daily', '25', '300001', '399999', '300001', 1, 'apy');

insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, start_num, end_num, next_num, step, card_type) values 
	('1','PST','daily master PST', 'daily', '25', '300001', '399999', '300001', 1, 'apy');

insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, card_type) values 
	('1','couriers','daily master of all couriers-LITE', 'daily', '0', 'lite');

insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, start_num, end_num, step, next_num, card_type) values 
	('1','PRF','daily LITE master PRF-LITE', 'daily', '53', '300001', '399999', '300001',1, 'lite');

insert into ventura.counters(isactive,counter_name, descript, freq_period, parent_id, start_num, end_num, step, next_num, card_type) values 
	('1','PST','daily LITE master PST-LITE', 'daily', '53', '300001', '399999', '300001',1, 'lite');


CREATE TABLE ventura.states (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	isactive	bool,
	code varchar(50) not null,
	name varchar(100) not null,
	country_code varchar(2),

	CONSTRAINT states_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

CREATE TABLE ventura.countries (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	isactive	bool,
	code varchar(2) not null,
	code3 varchar(3),
	name varchar(100) not null,

	CONSTRAINT countries_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

create table ventura.reject_reasons(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	lstid varchar(10) not null,
	short_desc varchaR(75),
	long_desc varchaR(150),
    CONSTRAINT reject_reasons_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

CREATE TABLE ventura.couriers(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	isactive	bool,
	code varchar(2) not null,
	code3 varchar(4),
	name varchar(100) not null,
	
	isvirtual	bool,
	awb_params varchar(40),
	sort_order integer DEFAULT 1,

	CONSTRAINT couriers_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

-- alter table ventura.couriers alter column code3 type varchar(4);

-- alter table ventura.couriers add awb_params varchar(40)
-- update ventura.couriers set awb_params = ''

--alter table ventura.couriers add isvirtual	bool; 
--update ventura.couriers set isvirtual = false;

--alter table ventura.couriers add sort_order integer  DEFAULT 1; 
--update ventura.couriers set sort_order = '1';

--insert into ventura.couriers(isactive,code, code3, name, isvirtual) values
--('1','D1','PRF','Prefered Courier TEST', false)
--, ('1','A1','PST','Post', false);

--insert into ventura.couriers(isactive,code, code3, name, isvirtual, awb_params) values
-- ('1','x1','PSTS','Speed Post', true, '86423597:11');
---------

/*
select * from ventura.fileinfo ;
select * from ventura.filedetails order by fileinfo_id, id;
select * from ventura.counters;
*/
-- delete from ventura.fileinfo;
-- ALTER SEQUENCE ventura.fileinfo_id_seq RESTART WITH 1;
-- delete from ventura.filedetails;
-- ALTER SEQUENCE ventura.filedetails_id_seq RESTART WITH 1;
-- delete from ventura.filedetail_actions;
-- ALTER SEQUENCE ventura.filedetail_actions_id_seq RESTART WITH 1;

--update ventura.counters set next_num=start_num where parent_id > 0;

-- DROP FUNCTION ventura.get_serial_number(character varying, character varying, character varying, boolean, integer,character varying, character varying);


CREATE OR REPLACE FUNCTION ventura.get_serial_number(
	master_type character varying,
	pdoc_val character varying,
	card_type_param character varying,
	init_if_need boolean,
	lock_id integer,
	freq_type character varying,
	freq_val character varying)
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
 
  master_start INTEGER;
  master_end INTEGER;
  master_step INTEGER;
  nx INTEGER;
begin
  lser_no := -1;
  lmaster_id := -1;
  master_start := -1;
  master_end := -1;
  master_step := 1;
  nx := 1;
  
  --find master record--
  if freq_type = '' then
    select id, coalesce(step, 1)
    into lmaster_id, master_step
    from ventura.counters
    where isactive='1' and counter_name = master_type and parent_id = 0 and coalesce(freq_period, '') = '' and coalesce(card_type, '') =card_type_param;
  else
    select id
    into lmaster_id
    from ventura.counters
    where isactive='1' and counter_name = master_type and parent_id = 0 and coalesce(freq_period,'') = freq_type and coalesce(card_type, '') =card_type_param;
	if found then  -- also find 2nd level master based on freq (daily) + what variation such as courier (PST / PRF)
	  select id, start_num, end_num, coalesce(step, 1) 
      into lmaster_id, master_start, master_end, master_step
      from ventura.counters
      where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and coalesce(freq_period, '') = freq_type and coalesce(card_type, '') =card_type_param;
	else
	  return '-11';  -- cannot add master or intermediate master rec
    end if;
  end if;
  
  if not found then  -- master rec not found
    if init_if_need = '1' then
	  if freq_type = '' then
	    insert into ventura.counters (isactive, counter_name, descript, parent_id, step, card_type)
 	    values ('1', master_type, 'auto create1', 0, 1, card_type_param) RETURNING id into lmaster_id;

		insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key, card_type)
 	    values ('1', pdoc_val, 'auto create2', lmaster_id, 1, 1, 99999, 2, lock_id, card_type_param);
		
 	    lser_no := 1;
	  else
	    return '-21';  -- daily or other periodical intermediate master missing
	  end if;
	else
		return '-31';  -- master rec not found, record was expected
    end if;
  else  --master rec found
	if freq_type = '' then
      select next_num, id 
      into lser_no, child_id
      from ventura.counters
      where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
	   and (lock_key = lock_id or lock_key <= 0 or lock_id <=0)
	   and coalesce(freq_period, '') = ''  -- if not frequency dependent - must be empty
	   and coalesce(card_type, '') =card_type_param
	  order by sort_order, start_num limit 1;
	else
      select next_num, id
      into lser_no, child_id
      from ventura.counters
      where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
	   and (lock_key = lock_id or lock_key <= 0 or lock_id <=0)
	   and freq_period = freq_val -- what period like exact date 20210801
	   and coalesce(card_type, '') =card_type_param
	  order by sort_order, start_num limit 1;
	end if;

    if found then -- child rec found
      update ventura.counters 
	  set next_num = lser_no + master_step
      where id =child_id;
 	else -- child rec not found
	  if init_if_need = '1' then
	    if freq_type = '' then
		  insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key, card_type)
 	      values ('1', pdoc_val, 'auto create3', lmaster_id, 1, 1, 99999, 2, lock_id, card_type_param);
		
 	      lser_no := 1;
		else
			nx := master_start + master_step;
			insert into ventura.counters (isactive, counter_name, descript, parent_id
				, start_num, step, end_num, next_num, lock_key, freq_period, card_type)
			values ('1', pdoc_val, 'auto create4', lmaster_id
				, master_start, master_step, master_end, nx , lock_id, freq_val, card_type_param);

			lser_no := master_start;
		end if; --by freq_type		
	  else
		return '-71'; -- child rec expected but not found
	  end if; -- can add 
	end if; -- child rec
	
 end if; --master rec

  return lser_no;
 
end;
$BODY$;

ALTER FUNCTION ventura.get_serial_number(character varying, character varying, character varying, boolean, integer, character varying, character varying)
    OWNER TO postgres;

-- FUNCTION: ventura.lock_counter(character varying, character varying, integer)

-- DROP FUNCTION ventura.lock_counter(character varying, character varying, character varying, integer, bit,character varying, character varying);

CREATE OR REPLACE FUNCTION ventura.lock_counter(
	master_type character varying,
	pdoc_val character varying,
	card_type_param character varying,
	lock_id integer,
	ok_to_add bit,
	freq_type character varying,
	freq_val character varying)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
	SET search_path FROM CURRENT
AS $BODY$
declare 
  lmaster_id INTEGER;
  child_id INTEGER;
  
  master_start INTEGER;
  master_end INTEGER;
  master_step INTEGER;
  cur_lock INTEGER;
begin
  lmaster_id := -1;
  child_id := -1;
  master_start := -1;
  master_end := -1;
  master_step := 1;
  cur_lock := -1;
  
  if freq_type = '' then
    select id 
    into lmaster_id
    from ventura.counters
    where isactive='1' and counter_name = master_type and parent_id = 0 and coalesce(freq_period, '') = '' and coalesce(card_type, '') =card_type_param;
  else
    select id 
    into lmaster_id
    from ventura.counters
    where isactive='1' and counter_name = master_type and parent_id = 0 and freq_period = freq_type and coalesce(card_type, '') =card_type_param;
	if found then
	  select id, start_num, end_num, step 
      into lmaster_id, master_start, master_end, master_step
      from ventura.counters
      where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and freq_period = freq_type and coalesce(card_type, '') =card_type_param;
	else
	  return -1;  -- cannot add master or intermediate master rec
    end if;
  end if;

  if found then  -- master rec found
    if freq_type = '' then
		select id, COALESCE(lock_key,0) 
		into child_id, cur_lock
		from ventura.counters
		where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num
		 and coalesce(freq_period, '') = ''
		 and coalesce(card_type, '') =card_type_param
		-- and COALESCE(lock_key,0) <= 0
		order by start_num limit 1;
	else
		select id, COALESCE(lock_key,0) 
		into child_id, cur_lock
		from ventura.counters
		where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
		 and freq_period = freq_val
		 and coalesce(card_type, '') =card_type_param
		 -- and COALESCE(lock_key,0) <= 0
		order by start_num limit 1;
    end if;

    if found then  -- child rec found
       if cur_lock > 0 and cur_lock <> lock_id then
          return -5;  --already locked
	   else
          update ventura.counters 
	      set lock_key = lock_id
          where id =child_id;
       end if;
	else
	  if ok_to_add = '1' then
	    	  
	    if freq_type = '' then
			insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key, card_type)
			values ('1', pdoc_val, 'auto create5', lmaster_id, 1, 1, 99999, 1, lock_id, card_type_param);

			select id 
			into child_id
			from ventura.counters
			where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
			and lock_key = lock_id and coalesce(freq_period, '') = '' and coalesce(card_type, '') =card_type_param;
		else
			insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key, freq_period, card_type)
			values ('1', pdoc_val, 'auto create6', lmaster_id, master_start, master_step, master_end, master_start, lock_id, freq_val, card_type_param);
		
			select id 
			into child_id
			from ventura.counters
			where isactive='1' and counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
			and lock_key = lock_id and freq_period = freq_val and coalesce(card_type, '') =card_type_param;
	    end if;  --freq_type

	  end if; -- ok to add rec
	end if;  -- child rec found
  else
    return -9;
  end if;

  return child_id;
 
end;
$BODY$;

ALTER FUNCTION ventura.lock_counter(character varying, character varying, character varying, integer, bit, character varying, character varying)
    OWNER TO postgres;

insert into ventura.states(country_code,name,code) values('IN','ANDAMAN AND NICOBAR ISLANDS','01');
insert into ventura.states(country_code,name,code) values('IN','ANDHRA PRADESH','02');
insert into ventura.states(country_code,name,code) values('IN','ARUNACHAL PRADESH','03');
insert into ventura.states(country_code,name,code) values('IN','ASSAM','04');
insert into ventura.states(country_code,name,code) values('IN','BIHAR','05');
insert into ventura.states(country_code,name,code) values('IN','CHANDIGARH','06');
insert into ventura.states(country_code,name,code) values('IN','DADRA & NAGAR HAVELI','07');
insert into ventura.states(country_code,name,code) values('IN','DAMAN & DIU','08');
insert into ventura.states(country_code,name,code) values('IN','DELHI','09');
insert into ventura.states(country_code,name,code) values('IN','GOA','10');
insert into ventura.states(country_code,name,code) values('IN','GUJARAT','11');
insert into ventura.states(country_code,name,code) values('IN','HARYANA','12');
insert into ventura.states(country_code,name,code) values('IN','HIMACHAL PRADESH','13');
insert into ventura.states(country_code,name,code) values('IN','JAMMU & KASHMIR','14');
insert into ventura.states(country_code,name,code) values('IN','KARNATAKA','15');
insert into ventura.states(country_code,name,code) values('IN','KERALA','16');
insert into ventura.states(country_code,name,code) values('IN','LAKHSWADEEP','17');
insert into ventura.states(country_code,name,code) values('IN','MADHYA PRADESH','18');
insert into ventura.states(country_code,name,code) values('IN','MAHARASHTRA','19');
insert into ventura.states(country_code,name,code) values('IN','MANIPUR','20');
insert into ventura.states(country_code,name,code) values('IN','MEGHALAYA','21');
insert into ventura.states(country_code,name,code) values('IN','MIZORAM','22');
insert into ventura.states(country_code,name,code) values('IN','NAGALAND','23');
insert into ventura.states(country_code,name,code) values('IN','ORISSA','24');
insert into ventura.states(country_code,name,code) values('IN','PONDICHERRY','25');
insert into ventura.states(country_code,name,code) values('IN','PUNJAB','26');
insert into ventura.states(country_code,name,code) values('IN','RAJASTHAN','27');
insert into ventura.states(country_code,name,code) values('IN','SIKKIM','28');
insert into ventura.states(country_code,name,code) values('IN','TAMILNADU','29');
insert into ventura.states(country_code,name,code) values('IN','TRIPURA','30');
insert into ventura.states(country_code,name,code) values('IN','UTTAR PRADESH','31');
insert into ventura.states(country_code,name,code) values('IN','WEST BENGAL','32');
insert into ventura.states(country_code,name,code) values('IN','CHHATISHGARH','33');
insert into ventura.states(country_code,name,code) values('IN','UTTARANCHAL','34');
insert into ventura.states(country_code,name,code) values('IN','JHARKHAND','35');
insert into ventura.states(country_code,name,code) values('IN','NRI (Foreign Address)','99');
insert into ventura.states(country_code,name,code) values('IN','Defense','88');

insert into ventura.countries(name, code, isactive) values 
('Argentina','AR','1')
,('Australia','AU','1')
,('Austria','AT','1')
,('Bahrain','BH','1')
,('Bangladesh','BD','1')
,('Barbados','BB','1')
,('Belarus','BY','1')
,('Belgium','BE','1')
,('Bermuda','BM','1')
,('Bhutan','BT','1')
,('Botswana','BW','1')
,('Brunei Darussalam','BN','1')
,('Bulgaria','BG','1')
,('Cambodia','KH','1')
,('Canada','CA','1')
,('Cape Verde','CV','1')
,('Cayman Islands','KY','1')
,('China','CN','1')
,('Cuba','CU','1')
,('Cyprus','CY','1')
,('Denmark','DK','1')
,('Egypt','EG','1')
,('El Salvador','SV','1')
,('Eritrea','ER','1')
,('Estonia','EE','1')
,('Ethiopia','ET','1')
,('Fiji','FJ','1')
,('France','FR','1')
,('Georgia','GE','1')
,('Germany','DE','1')
,('Ghana','GH','1')
,('Greece','GR','1')
,('Guyana','GY','1')
,('Hong Kong','HK','1')
,('Hungary','HU','1')
,('Iceland','IS','1')
,('India','IN','1')
,('Indonesia','ID','1')
,('Iran, Islamic Republic of','IR','1')
,('Iraq','IQ','1')
,('Ireland','IE','1')
,('Israel','IL','1')
,('Italy','IT','1')
,('Japan','JP','1')
,('Jordan','JO','1')
,('Kenya','KE','1')
,('Korea, Democratic People''s Republic of','KP','1')
,('Kuwait','KW','1')
,('Latvia','LV','1')
,('Luxembourg','LU','1')
,('Macao','MO','1')
,('Malawi','MW','1')
,('Malaysia','MY','1')
,('Maldives','MV','1')
,('Mauritius','MU','1')
,('Mexico','MX','1')
,('Mongolia','MN','1')
,('Morocco','MA','1')
,('Namibia','NA','1')
,('Nauru','NR','1')
,('Nepal','NP','1')
,('Netherlands','NL','1')
,('New Zealand','NZ','1')
,('Niger','NE','1')
,('Nigeria','NG','1')
,('Norway','NO','1')
,('Oman','OM','1')
,('Pakistan','PK','1')
,('Panama','PA','1')
,('Papua New Guinea','PG','1')
,('Philippines','PH','1')
,('Poland','PL','1')
,('Portugal','PT','1')
,('Qatar','QA','1')
,('Romania','RO','1')
,('Russian Federation','RU','1')
,('Rwanda','RW','1')
,('Saudi Arabia','SA','1')
,('Senegal','SN','1')
,('Singapore','SG','1')
,('South Africa','ZA','1')
,('Spain','ES','1')
,('Sri Lanka','LK','1')
,('Sudan','SD','1')
,('Sweden','SE','1')
,('Switzerland','CH','1')
,('Taiwan, Province of China','TW','1')
,('Tanzania, United Republic of','TZ','1')
,('Thailand','TH','1')
,('Tunisia','TN','1')
,('Turkey','TR','1')
,('Uganda','UG','1')
,('Ukraine','UA','1')
,('United Arab Emirates','AE','1')
,('United Kingdom','GB','1')
,('United States of America','US','1')
,('Viet Nam','VN','1')
,('Yemen','YE','1')
,('Zair','ZR','1')
,('Zimbabwe','ZW','1')

--
insert into ventura.reject_reasons (lstid,short_desc,long_desc) values 
('SIGH','Reject-Problem in Signature (Pri Print Hold)','Signature mismatch/ Signature not clear raise this status. Applicable to only PRAN (Pri Print Hold)')
,('SIG','Reject-Problem in Signature (Post Print Hold)','Signature mismatch/ Signature not clear raise this status. Applicable to only PRAN (Post Print Hold)')
,('REH','Second time hold by printer','Put on hold by printer after release instruction given')
,('PHOH','Reject-Problem in Photo (Pri Print Hold)','Photo mismatch/ Photo view not clear raise this status. Applicable to only PRAN (Pri Print Hold)')
,('PHO','Reject-Problem in Photo (Post Print Hold)','Photo mismatch/ Photo view not clear raise this status. Applicable to only PRAN (Post Print Hold)')
,('NSH','Hold by NSDL','Hold due to request by NSDL')
,('MUL','Reject-Due to multiple reasons','Hold of PRAN/PIN due to multiple reasons, this status is raised. NOTE: This status is raised only by LCM and not by printer')
,('FATH','Reject-Problem in Fathers''s Name (Pri Print Hold) ','Father''s Name Insufficient/ Father''s Name incorrect will raise this status. Applicable to only PRAN (Pri Print Hold)')
,('FAT','Reject-Problem in Fathers''s Name (Post Print Hold)','Father''s Name Insufficient/ Father''s Name incorrect will raise this status. Applicable to only PRAN (Post Print Hold)')
,('DOIH','Reject-Problem Date of Birth/Date of Incorporation (Pri Print Hold) ','Incorrect date of birth/ date of incorporation will raise this status. Applicable to only PRAN (Pri Print Hold)')
,('DOI','Reject-Problem Date of Birth/Date of Incorporation (Post Print Hold)','Incorrect date of birth/ date of incorporation will raise this status. Applicable to only PRAN (Post Print Hold)')
,('APPH','Reject-Problem in Applicant''s Name (Pri Print Hold)','Name Insufficient/ Name incorrect will raise this status. Applicable to both PRAN/PIN (Pri Print Hold)')
,('APP','Reject-Problem in Applicant''s Name (Post Print Hold) ','Name Insufficient/ Name incorrect will raise this status. Applicable to both PRAN/PIN (Post Print Hold)')
,('ADDH','Reject-Problem in Address (Pri Print Hold)','Address insufficient/ Wrong Pincode/ Address line repeated raise this status. Applicable to both PRAN/PIN (Pri Print Hold)')
,('ADD','Reject-Problem in Address (Post Print Hold) ','Address insufficient/ Wrong Pincode/ Address line repeated raise this status. Applicable to both PRAN/PIN (Post Print Hold)')
,('HIN','Hold By Printer-Incorrect Hindi Name','Hold due to Incorrect Hindi Name')

