$DIR/../runtime

-- meta data of columns
-- data_type are: STRING, INT, DATE : upper case needed
--
insert into ventura.column_meta (name, data_type, mandatory, length_or_range, comment) values
('first_name', 'STRING','0','50',''),
('last_name', 'STRING','1','50','must have last name'),
('dob', 'DATE','0','1950/01/01-','date of birth'),
('service_length', 'INT','0','0-50','')
;

insert into ventura.read_template(client_id, template_name, created_by, created_date) values ('1', 'test 1', 'sql', '2021/05/19');

--input index is 0 based
-- file : id, first_name, last, email, dob
insert into ventura.read_template_det(template_id, input_index, input_col_header, output_column_id) values
('1',1,'First Name', 1),
('1',2,'Last Name', 2),
('1',4,'birth date', 3)
;

insert into ventura.read_job(url, output_path, status, created_by, created_date, client_id, read_template_id, priority) values
('sftp://someserver.domain.com', 'c:\zunk','PENDING', 'sql', '2021/05/19', '1', '1', '2')

/* update ventura.main_data set child_json 
= '{"batch":"120001", "seq":"002", "ch":{"fname":"Ana","deg":"BE"}, "addr":{"street":"1/1 Unnat nagar","city":"Goregaon"} }' 
where id='3';
*/

select first_name, child_json -> 'seq', child_json ->'ch'->'fname' from ventura.main_data;


