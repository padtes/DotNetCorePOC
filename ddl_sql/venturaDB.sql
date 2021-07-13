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
, "printer_code3":"XYZ", "printer_code2":"51"}');


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
values('1','lite_resp','lite','lite_imm_resp.json','PRN{{sys_param(printer_code)}}RES{{now_ddmmyy}}{{Serial No}}.txt');

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
	addeddate	timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT counters_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

insert into ventura.counters(isactive,counter_name,parent_id) values ('1','generic',0)
--"couriers" master rec for couriers
--"detail rec for couriers"

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
	code3 varchar(3),
	name varchar(100) not null,

	CONSTRAINT couriers_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

--insert into ventura.couriers(isactive,code, code3, name) values
--('1','D1','PRF','Prefered Courier TEST')
--, ('1','A1','PST','Post TEST');

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
      update ventura.counters 
	  set next_num = lser_no + 1
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
	lock_id integer,
	ok_to_add bit)
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
	else
	  if ok_to_add = '1' then
	  	insert into ventura.counters (isactive, counter_name, descript, parent_id, start_num, step, end_num, next_num, lock_key)
 	    values ('1', pdoc_val, 'auto create', lmaster_id, 1, 1, 99999, 1, lock_id);

		select id 
		into child_id
		from ventura.counters
		where counter_name = pdoc_val and parent_id = lmaster_id and next_num <= end_num 
		 and lock_key = lock_id;
	  end if; 
	end if;
  end if;

  return child_id;
 
end;
$BODY$;

ALTER FUNCTION ventura.lock_counter(character varying, character varying, integer)
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

