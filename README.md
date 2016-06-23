#### Use:
	spm <DBName> <-p|-i <mode>|-v|-con> <packed file>
	
#### Example:
	spm MyDB -p webdb.zip

will save all SPs in MyDB to packed file 'webdb.zip'

| Item | Description |
| :--- | :--- |
| DBName  | name of Database  |
| -p  | to packed all SPs  |
| -i <mode> | to install SP; <mode> = {all, new, replace} |
| -v  | to validate what SPs in DB that diffent from SPs in packed file |
| -con | interactive console mode |