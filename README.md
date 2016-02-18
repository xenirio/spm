#### Use:
	spm <DBName> <-p|-i|-v> <packed file>
	
#### Example:
	spm MyDB -p webdb.zip

will save all SPs in MyDB to packed file 'webdb.zip'

| Item | Description |
| :--- | :--- |
| DBName  | name of Database  |
| -p  | to packed all SPs  |
| -i  | to install all SPs (replace if exist)  |
| -si  | to safety install (won't replace at all)  |
| -v  | to validate what SPs in DB that diffent from SPs in packed file  |