USER IMPORT PIPELINE (JSON → DATABASE)
====================================

This folder contains scripts to import users from users.json
and handle duplicates safely without breaking the main database.

------------------------------------
FOLDER OVERVIEW
------------------------------------

Scripts/
│
├─ MobyPark.Tools/
│   ├─ Program.cs
│   ├─ MobyPark.Tools.csproj
│   ├─ appsettings.json
│   ├─ users.json
│   └─ split_duplicates.py
│
├─ DuplicateUsers/
│   ├─ Program.cs
│   ├─ DuplicateUsers.csproj
│   └─ appsettings.json
│
└─ README.txt   ← current file

------------------------------------
STEP 1 — IMPORT USERS.JSON
------------------------------------

This imports users.json into the Users table.
Valid users are inserted.
Duplicates + invalid rows are written to jsonl files.

1. Open PowerShell
2. Go to MobyPark.Tools

   cd Scripts\MobyPark.Tools

3. Run the importer

   dotnet run -- users.json

Output files created:
- users.duplicates.jsonl
- users.rejected.jsonl

------------------------------------
STEP 2 — SPLIT DUPLICATES
------------------------------------

This splits users.duplicates.jsonl into categories:
- by email
- by username
- unknown (both need fixing)

1. Still inside MobyPark.Tools
2. Run:

   python split_duplicates.py

Output folder:
- SplitResults/

Files inside:
- users.duplicates.by_email.jsonl
- users.duplicates.by_username.jsonl
- users.duplicates.unknown.jsonl

------------------------------------
STEP 3 — IMPORT DUPLICATES INTO DB
------------------------------------

This imports duplicate users into the DuplicateUsers table
for manual fixing via the API later.

1. Go to DuplicateUsers folder

   cd ..\DuplicateUsers

2. Import each file (run all three):

   dotnet run -- "..\MobyPark.Tools\SplitResults\users.duplicates.by_email.jsonl"

   dotnet run -- "..\MobyPark.Tools\SplitResults\users.duplicates.by_username.jsonl"

   dotnet run -- "..\MobyPark.Tools\SplitResults\users.duplicates.unknown.jsonl"

------------------------------------
DATABASE SAFETY
------------------------------------

- Users table = clean users only
- DuplicateUsers table = users that need fixing
- No existing data is deleted
- Original JSON values are preserved

------------------------------------
PASSWORD HANDLING
------------------------------------

- Legacy passwords are stored as MD5 in LegacyPasswordHash
- PasswordHash uses bcrypt
- On first successful login:
  → legacy password is verified
  → password is upgraded to bcrypt automatically

------------------------------------
TROUBLESHOOTING
------------------------------------

If you see build errors:
- Run `dotnet restore`
- Check appsettings.json connection string
- Make sure DuplicateUsers.csproj references MobyPark.csproj

------------------------------------
DONE
------------------------------------

After this:
- Use the DuplicateUsers API to resolve duplicates
- Resolved users are automatically added to Users table
