---
theme: espc25
layout: default
---

# Recap: Teams Interoperability

1. Graph API creates a Teams meeting in the agent's Outlook calendar
2. Customer is added as an attendee (gets email invite)
3. Stores the meeting link and event ID in the Cosmos DB chat thread document.
4. Sends a chat message to the customer with the meeting join link
5. Customer sees this in their ACS chat window
6. They can click the link to join the Teams meeting