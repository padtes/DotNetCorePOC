select field
, lower(replace(replace(replace(replace(replace(replace(replace(replace(replace
		(trim(field), ' ', '_'),'/','_'),'(','_'), '''',''),'’',''),'.',''),')',''), '___', '_'), '__', '_')
	   ) 
, lower(replace(replace(
	replace(replace(replace(translate(trim(field), '-’''.,&)', ''), ' ','_'), '/','_'), '(', '_') 
	,'___', '_'), '__', '_')) f1

 as new_field1
from pocventura.zz_nps_input_excel;

-- Table: pocventura.column_meta

-- DROP TABLE pocventura.column_meta;

CREATE TABLE pocventura.column_meta
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    name character varying(50) COLLATE pg_catalog."default" NOT NULL,
    data_type character varying(10) COLLATE pg_catalog."default" NOT NULL,
    mandatory bit(1) NOT NULL,
    length_or_range character varying(21) COLLATE pg_catalog."default" NOT NULL,
    comment character varying(40) COLLATE pg_catalog."default",
    CONSTRAINT column_meta_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE pocventura.column_meta
    OWNER to postgres;

COMMENT ON TABLE pocventura.column_meta
    IS 'Column meta data for Raw Data';

COMMENT ON COLUMN pocventura.column_meta.comment
    IS 'descibe the column, specially if duplicated with different rule';

--
-- Table: pocventura.read_template

-- DROP TABLE pocventura.read_template;

CREATE TABLE pocventura.read_template
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    client_id integer,
    template_name character varying(40) COLLATE pg_catalog."default" NOT NULL,
    created_by character varying(50) COLLATE pg_catalog."default" NOT NULL,
    created_date date NOT NULL,
    CONSTRAINT "File_def_pkey" PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE pocventura.read_template
    OWNER to postgres;
--

-- Table: pocventura.read_template_det

-- DROP TABLE pocventura.read_template_det;

CREATE TABLE pocventura.read_template_det
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    template_id integer NOT NULL,
    input_index integer NOT NULL,
    input_col_header character varying(45) COLLATE pg_catalog."default" NOT NULL,
    output_column_id integer NOT NULL,
    CONSTRAINT read_template_det_pkey PRIMARY KEY (id),
    CONSTRAINT "FK_output_column_id_meta" FOREIGN KEY (output_column_id)
        REFERENCES pocventura.column_meta (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE pocventura.read_template_det
    OWNER to postgres;

-- Table: pocventura.read_job

-- DROP TABLE pocventura.read_job;

CREATE TABLE pocventura.read_job
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    url character varying(255) COLLATE pg_catalog."default" NOT NULL,
    output_path character varying(255) COLLATE pg_catalog."default" NOT NULL,
    status character varying(15) COLLATE pg_catalog."default" NOT NULL,
    created_by character varying(50) COLLATE pg_catalog."default" NOT NULL,
    created_date timestamp without time zone NOT NULL,
    client_id integer,
    read_template_id integer NOT NULL,
    status_row_num integer,
    priority integer,
    last_upd_date timestamp without time zone,
    CONSTRAINT read_job_pkey PRIMARY KEY (id),
    CONSTRAINT "FK_read_template_id_id" FOREIGN KEY (read_template_id)
        REFERENCES pocventura.read_template (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE pocventura.read_job
    OWNER to postgres;

COMMENT ON TABLE pocventura.read_job
    IS 'First step to read a file';

COMMENT ON COLUMN pocventura.read_job.output_path
    IS 'output path';

COMMENT ON COLUMN pocventura.read_job.client_id
    IS 'TBD: FK from Client table';

COMMENT ON COLUMN pocventura.read_job.read_template_id
    IS 'FK of read_template';

COMMENT ON COLUMN pocventura.read_job.status_row_num
    IS 'if error or write aborts at some row - keep it here';

COMMENT ON COLUMN pocventura.read_job.priority
    IS 'lower number get processed first';

--

-- Table: pocventura.main_data

-- DROP TABLE pocventura.main_data;

CREATE TABLE pocventura.main_data
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    job_id integer NOT NULL,
    row_number integer NOT NULL,
    first_name character varying(50) COLLATE pg_catalog."default",
    last_name character varying(50) COLLATE pg_catalog."default",
    dob date,
    service_length integer,
    child_json jsonb,
    batch_id character varying(12) COLLATE pg_catalog."default",
    ack_number character varying(18) COLLATE pg_catalog."default",
    pran_id character varying(12) COLLATE pg_catalog."default",
    photograph character varying(10485760) COLLATE pg_catalog."default",
    signature character varying(10485760) COLLATE pg_catalog."default",
    CONSTRAINT main_data_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE pocventura.main_data
    OWNER to postgres;
---
create table pocventura.serial_number 
(
	id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
	ser_no integer,
	doc_type character varying(15) COLLATE pg_catalog."default",
	CONSTRAINT serial_number_pkey PRIMARY KEY (id)
);

---
create function get_serial_number(pdoc_type character varying(15))
returns int
language plpgsql
as
$$
declare 
  lser_no integer;
begin
  lser_no := -1;
  select ser_no 
  into lser_no
  from pocventura.serial_number
  where doc_type = pdoc_type;
  
  if not found then
	insert into pocventura.serial_number (ser_no, doc_type)
	values (1, pdoc_type);
	lser_no := 1;
  else
    lser_no := lser_no + 1;
	update pocventura.serial_number set ser_no = lser_no, doc_type = pdoc_type;
  end if;

  return lser_no;
 
end;
$$;

---
create or Replace function get_subscriber_addr(ppran_id character varying(12))
returns character varying(200)
language plpgsql
as
$$
declare 
  laddr character varying(200);
begin
  laddr := '';
  select child_json->'PD'->0->'P017_subscriber_address_line_1' 
  into laddr
  from pocventura.main_data
  where pran_id = ppran_id;
  
  if not found then
	  select child_json->'CD'->0->'C016_correspondence_overseas_address_line_1' 
	  into laddr
	  from pocventura.main_data
	  where pran_id = ppran_id;
  end if;

  return laddr;
 
end;
$$;
