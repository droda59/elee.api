-----------
  READ ME  
-----------
This is what you need to know and do to run this. 

--------------
  SERVER API
--------------

Preparation
-----------
To be able to run the server api, you need to do the following: 

- Install MongoDB
	https://www.mongodb.org/downloads#production
	Current release is 3.0.5
	
- RTFM
	http://docs.mongodb.org/manual/tutorial/install-mongodb-on-windows/
	Configure a Windows Service for MongoDB: 
		In Admin command prompt: 
			Create db and log folders ("mkdir C:\dev\mongodb\db" and "mkdir C:\dev\mongodb\log")
		In the MongoDB installation folder, create a mongod.cfg file with the following actual content: 
			systemLog:
				destination: file
				path: c:\dev\mongodb\log\mongod.log
			storage:
				dbPath: c:\dev\mongodb\db
		In Admin commant prompt: 
			sc.exe create MongoDB binPath= "C:\mongodb\bin\mongod.exe --service --config=\"C:\mongodb\mongod.cfg\"" DisplayName= "MongoDB" start= "auto"
			All quotes and double quotes are important as-is. This will install the mongodb service and start it automatically at startup. 
			
			
Executing the app
-----------------
To run the API, run the Visual Studio solution. It contains a REST API that you can use to test and develop. 
To use it, open a REST client like Postman or https://chrome.google.com/webstore/detail/advanced-rest-client/hgmloofddffdnphfgcellkdfbfbjeloo. 

- ParserTestController
	/parsertest/get: Returns the list of recipes in the database in SearchResult format (id, name, url)
	ex. GET http://localhost:50817/api/parsertest
	
	/parsertest/get/{id}: Returns the recipe with the id in the database, if it exists
	ex. GET http://localhost:50817/api/parsertest/e879fde3-9bf3-47c9-bbb8-03b163a45ca8
	
	/parsertest/put?url={url}: Parses the url for a known recipe, inserts it in the database, and returns it. 
	ex. PUT http://localhost:50817/api/parsertest/parse?url=http://www.ricardocuisine.com/recettes/5409-pouding-au-chocolat
