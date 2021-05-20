-- Table: ventura.column_meta

-- DROP TABLE ventura.column_meta;

CREATE TABLE ventura.column_meta
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

ALTER TABLE ventura.column_meta
    OWNER to postgres;

COMMENT ON TABLE ventura.column_meta
    IS 'Column meta data for Raw Data';

COMMENT ON COLUMN ventura.column_meta.comment
    IS 'descibe the column, specially if duplicated with different rule';

--
-- Table: ventura.read_template

-- DROP TABLE ventura.read_template;

CREATE TABLE ventura.read_template
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    client_id integer,
    template_name character varying(40) COLLATE pg_catalog."default" NOT NULL,
    created_by character varying(50) COLLATE pg_catalog."default" NOT NULL,
    created_date date NOT NULL,
    CONSTRAINT "File_def_pkey" PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE ventura.read_template
    OWNER to postgres;
--

-- Table: ventura.read_template_det

-- DROP TABLE ventura.read_template_det;

CREATE TABLE ventura.read_template_det
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    template_id integer NOT NULL,
    input_index integer NOT NULL,
    input_col_header character varying(45) COLLATE pg_catalog."default" NOT NULL,
    output_column_id integer NOT NULL,
    CONSTRAINT read_template_det_pkey PRIMARY KEY (id),
    CONSTRAINT "FK_output_column_id_meta" FOREIGN KEY (output_column_id)
        REFERENCES ventura.column_meta (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE ventura.read_template_det
    OWNER to postgres;

-- Table: ventura.read_job

-- DROP TABLE ventura.read_job;

CREATE TABLE ventura.read_job
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
        REFERENCES ventura.read_template (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE ventura.read_job
    OWNER to postgres;

COMMENT ON TABLE ventura.read_job
    IS 'First step to read a file';

COMMENT ON COLUMN ventura.read_job.output_path
    IS 'output path';

COMMENT ON COLUMN ventura.read_job.client_id
    IS 'TBD: FK from Client table';

COMMENT ON COLUMN ventura.read_job.read_template_id
    IS 'FK of read_template';

COMMENT ON COLUMN ventura.read_job.status_row_num
    IS 'if error or write aborts at some row - keep it here';

COMMENT ON COLUMN ventura.read_job.priority
    IS 'lower number get processed first';

--

-- Table: ventura.main_data

-- DROP TABLE ventura.main_data;

CREATE TABLE ventura.main_data
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    job_id integer NOT NULL,
    row_number integer NOT NULL,
    first_name character varying(50) COLLATE pg_catalog."default",
    last_name character varying(50) COLLATE pg_catalog."default",
    dob date,
    service_length integer,
    CONSTRAINT main_data_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE ventura.main_data
    OWNER to postgres;

