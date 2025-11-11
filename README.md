# ManjuEnsekTechAssesment 

This README file offers comprehensive details about the testing process I conducted for the assessment.

**Automation Testing : EnsekSeleniumTest/EnergyPurchaseTests.cs File**

Git Repository : (https://github.com/CodeWithManju/TechAssessment.git)

Programming Language: C# | Testing Framework: xUnit | Web Driver: Selenium WebDriver with ChromeDriver | IDE: Visual Studio Code

Automation testing Approach: Described in EnergyPurchaseTests.cs File

Test Case 1: Verify Home Page UI Elements

  Description: Validates the home page title, headers, paragraph, and buttons.

  Status: Passed

Test Case 2: Navigate to About Section

  Description: Checks navigation to About page and clicking "Find out more" button.

  Status: Passed

Test Case 3: Buy All Energy Types

  Description: Buys 50 units of Gas, Electricity, and Oil.

  Status: Passed

Test Case 4: Reset Buy Energy Units

  Description: Buys sample energy units and clicks Reset to verify fields are cleared.

  Status: Failed due to element location issues.

Results Storage:

  All test results are logged in EnsekTestResults.txt.

  Logs include success, failure, and errors with timestamps.

**Manual testing : - Manual Testing/Test Strategy and TestPlanDocs Folder**

1.**Test Approach Document for Ensek Test Website - Manju.doc**
    This document provides the complete test strategy and approach for the tech assignment.

2.**Ensek - Test Plan Document for Manual Testing - Manju.xlsx**
    This Excel Document “Ensek – Test Plan Document for Manual testing” which has 3 sheets named Test Coverage, Test Cases and Bug Lists.
    
    1. **Test Coverage Sheet** – Overview of number of test cases identiKied which includes the details of priority, pass and failure test cases.
    
    2. **Test Case Sheet** – Test cases identiKied with respect to Unit Testing, Functional Testing, Performance Testing, User Experience Testing, Cross Browser Testing and Exploratory Testing
    
    3. **Bug List** – Bugs IdentiKied while performing the manual testing which includes the screenshots for the issues.

3. **Error - Screenshots**
    This folder contains all the erors identified while performing the manual testing. 
  
# Energy API Tests - RESTAPITestSwaggerDoc

Base URL:
https://qacandidatetest.ensek.io/ENSEK

Test Cases covered

Test Case 1:Reset Data

  APITest: Checks if the /reset endpoint returns a 401 Unauthorized status when accessed without proper credentials.
  Expected Result: 401 Unauthorized

Test Case 2:Buy Energy

  APITest: Gets available energy types and buys a quantity (10) of each one. It checks if the purchase goes through successfully and saves the order IDs for later.
  Expected Result: 200 OK

Test Case 3:Get Orders
  
  APITest: Retrieves all orders and verifies that the orders we bought earlier are included. It also checks that each order has the right details.
  Expected Result: 200 OK

Test Case 4:Count Orders Before Current Date
  
  APITest: Counts how many orders were created before today and prints the number. It ensures that the order date was retrieved correctly.
  Expected Result: Displays the count of orders before the current date.

Test Case 5:Unauthorized Login
  
  APITest: Attempts to log in with the wrong credentials to see if it returns a 401 Unauthorized message.
  Expected Result: 401 Unauthorized with the message "Unauthorized"

Test Case 6:Bad Request on Buy
  
  APITest: Tries to buy an invalid quantity (like -5) and checks if the API responds with a 400 Bad Request error.
  Expected Result: 400 Bad Request with the message "Bad Request"

Test Results Stored In: surefire-reports

From the output of your Maven test run, here's a summary of the test results:

Total Tests Run: 7
  Passed: 4 (since 7 total tests - 2 failures - 1 error = 4 passed)
  Failed: 2
  Errors: 1
  Skipped: 0
