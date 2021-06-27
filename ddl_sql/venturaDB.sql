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
'{"inputdir":"c:/zunk/lite/input", "output_par":"nps_lite", "output_lite":"NPSLite", "output_apy":"APY"
, "workdir":"c:/zunk/lite/work", "systemdir":"c:/users/spadte/source/repos/padtes/DotNetCorePOC/ddl_sql"}');


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

