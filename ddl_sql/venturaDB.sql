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
values ('lite', 'directories'
 , '{"inputdir":"c:/zunk/lite/input", "workdir":"c:/zunk/lite/work", "systemdir":"c:/users/spadte/source/repos/padtes/DotNetCorePOC/ddl_sql"}')
;
