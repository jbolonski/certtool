# Application Requirements

## Requirements

The following table outlines the detailed functional requirements.

|Requirement ID | Description                         | User Story                                                    | Expected Behavor/Outcome |
|---------------|-------------------------------------|---------------------------------------------------------------|--------------------------|
|FR001          | Report of certificate expirations   | As a user, I want to be able to track certificate expirations | The system should provide a way for the user to see a list of certificates and their expiration dates|
|FR002          | Manage list of hosts to monitor     | As a user, I want to be able to manage the list of hosts that I monitor for certificate expiration | The system should provide an admin page or mechanism for manage the list of hosts to monitor|
|FR003          | Check the certficiates daily        | As a user, I want the certificates to be checked automatically on a daily basis | The system should schedule a task to automatically run through the list of hosts and retrieve the expiration information for the certificate |
|FR004          | Certificate Details should be shown | As a user, I want to be able to see the following certificate details: host name, serial number, expiration date | The system should capture the certificate details daily and display them on a report |
|FR005          | Reporting page should be tabular    | As a user, I want to see the certificate scan results in a table with the Certification name, serial number, expiration date, and days until expiring | The system should display the scan results in a reportable table with the appropriate headings. The host name and serial number can be in the same cell with the host name being the most dominate|
|FR006          | Reporting table sorted by expiration| As a user, I want to have the results sorted by days until expiration from lowest to highest. |

