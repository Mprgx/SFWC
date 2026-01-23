===========================================================================================================

DATA MIGRATION INSTRUCTIONS FOR PARKING LOTS AND PAYMENTS

===========================================================================================================
Requirements: SQL Server Express installed, MobyParkDb instantiated and updated (preferably empty), Windows pc.
===========================================================================================================

To migrate the parking lots and payments to the Mobypark SQL database, first read the other readme on how to seed the
Users table. 

===========================================================================================================

After seeding the Users table, then you should download the .sql file from
https://hrnl.sharepoint.com/:u:/s/CMI-CMI-INF2B2025-2026-Team4/IQBSz4hgM830RqT9iYam1j2GAZNWRFKNYFHQetv9-m7_GWA?e=YuB8Y6 .

After downloading the zip and extracting it, you should start the terminal as an admin and use the following command:

sqlcmd -S .\SQLEXPRESS -d MobyParkDb -i "FILE-PATH\script.sql" -a 32767 -C

Replace FILE-PATH with the file path where you've saved script.sql.

This will start seeding the staging tables in the database. The staging tables are a place to store the data as-is, before
changing types etc. 

After the script is finished (5-15mins), the staging tables are ready for use.

===========================================================================================================
PARKING LOTS

Start the API and login as an Admin. Then navigate to the Migration endpoint.

Firstly, run the /api/migrate/parking-lots/start​ endpoint. This will start seeding the parking lots.
This MUST be done before running the payments migration endpoint.

After the endpoint has finished seeding the database, which 200 OK will indicate, you can seed the Payments table.

===========================================================================================================
PAYMENTS (PARKING LOTS MUST BE DONE BEFORE THIS ONE!)

Navigate to the /api/migrate/payments/start endpoint, and run it as an admin also. This will return 202.
The payments take a while to seed, so you can check the terminal where the API is running, and it will show
its progress in steps of 50k. The payments data has around 2.7 million entries, so it can take around 5-15 minutes to seed the db.

Once the Payments data migration is done, it will show the line "Migration done." in the terminal where the API is running.

After this, you should've successfully migrated ParkingLots and Payments to the MobyParkDb.

===========================================================================================================