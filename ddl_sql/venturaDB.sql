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
values ('lite', 'directories',
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
	addedate	TIMESTAMP,
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
                        cmd.Parameters.AddWithValue("@fname", theFile.fname);

                        cmd.Parameters.AddWithValue("@fname", theFile.fname);
                        cmd.Parameters.AddWithValue("@fpath", theFile.fpath);
                        cmd.Parameters.AddWithValue("@fsize", theFile.fsize);
                        cmd.Parameters.AddWithValue("@biztype ", theFile.biztype);
                        cmd.Parameters.AddWithValue("@module_name", theFile.module_name);
                        cmd.Parameters.AddWithValue("@direction", theFile.direction);
                        cmd.Parameters.AddWithValue("@importedfrom", theFile.importedfrom);
                        cmd.Parameters.AddWithValue("@courier_sname", theFile.courier_sname);
                        cmd.Parameters.AddWithValue("@courier_mode", theFile.courier_mode);
                        cmd.Parameters.AddWithValue("@nprodrecords", theFile.nprodrecords);
                        cmd.Parameters.AddWithValue("@archivepath", theFile.archivepath);
                        cmd.Parameters.AddWithValue("@archiveafter", theFile.archiveafter);
                        cmd.Parameters.AddWithValue("@purgeafter", theFile.purgeafter);
                        cmd.Parameters.AddWithValue("@addedate", theFile.addedate);
                        cmd.Parameters.AddWithValue("@addedby", theFile.addedby);
                        cmd.Parameters.AddWithValue("@addedfromip", theFile.addedfromip);
                        cmd.Parameters.AddWithValue("@updatedate", theFile.updatedate);
                        cmd.Parameters.AddWithValue("@updatedby", theFile.updatedby);
                        cmd.Parameters.AddWithValue("@updatedfromip", theFile.updatedfromip);
                        cmd.Parameters.AddWithValue("@isdeleted", theFile.isdeleted);
                        cmd.Parameters.AddWithValue("@inp_rec_status", theFile.inp_rec_status);
                        cmd.Parameters.AddWithValue("@inp_rec_status_d", theFile.inp_rec_status_d);







