# WorkDay-SOAP-API-ConsoleApp
Console App written in C# to get data from WorkDay Get-Workers SOAP API and save as xml files

Maintained and shared by Minnesota State University Mankato. Please feel free to use, however we do not provide any support for the code.


Nik Nik Hassan  
Minnesota State University Mankato  
2/6/2023

## Files
1. AccessToken.cs
Definitions for the access token, currently only access_token is used

2. Program.cs
Console app that is based on Jeremy Heydmann's Groovy file.
Gets first two pages of worker data and saves them into two XML files
Each file contains 50 records

## Variables
#### These variable will be provided by the SO
* wdUsername - username of institution
* clientId - client ID of institution
* clientSecret - secret key of institution
* refreshToken - refresh token of institution

#### This variable is user defined
resultsPerPage - number of results per page