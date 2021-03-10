insert into t_department (department_id,department_name,parent_id,level,description) values (1, '后勤部', 0, 1, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (2, '财务科', 1, 2, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (3, '食堂', 2, 3, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (4, '保卫科', 3, 4, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (5, '图书馆', 4, 5, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (6, '仓库', 5, 6, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (7, '机房', 6, 7, '');
insert into t_department (department_id,department_name,parent_id,level,description) values (8, '幼儿园', 7, 8, '');

insert into t_position (position_id, position_name, description, level) values (1, '经理', '', 0);
insert into t_position (position_id, position_name, description, level) values (2, '员工', '', 1);