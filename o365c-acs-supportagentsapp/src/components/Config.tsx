import { useEffect } from "react";
import { app, pages } from "@microsoft/teams-js";
import {
  Text,
  Card,
  CardHeader,
  Body1,
  Caption1,
} from "@fluentui/react-components";

const Config: React.FC = () => {
  useEffect(() => {
    console.log("Config component mounted");
    
    // Initialize Teams SDK
    app.initialize().then(() => {
      console.log("Teams SDK initialized in config page");

      /**
       * When the user clicks "Save", save the url for your configured tab.
       * This allows for the addition of query string parameters based on
       * the settings selected by the user.
       */
      pages.config.registerOnSaveHandler((saveEvent) => {
        console.log("Save handler triggered");
        const baseUrl = `https://${window.location.hostname}:${window.location.port}`;
        console.log("Base URL:", baseUrl);
        
        const config = {
          suggestedDisplayName: "Support Agents",
          entityId: "supportAgentsTeamTab",
          contentUrl: baseUrl + "/index.html#/tab",
          websiteUrl: baseUrl + "/index.html#/tab",
        };
        
        console.log("Setting config:", config);
        
        pages.config
          .setConfig(config)
          .then(() => {
            console.log("Configuration saved successfully");
            saveEvent.notifySuccess();
          })
          .catch((error) => {
            console.error("Error saving configuration:", error);
            saveEvent.notifyFailure("Failed to save configuration: " + error.message);
          });
      });

      /**
       * After verifying that the settings for your tab are correctly
       * filled in by the user you need to set the state of the dialog
       * to be valid. This will enable the save button in the configuration
       * dialog.
       */
      pages.config.setValidityState(true);
      console.log("Validity state set to true");

      console.log("Configuration setup completed");
    }).catch((error) => {
      console.error("Error initializing Teams SDK:", error);
      
      // Fallback: try to set validity state anyway
      try {
        pages.config.setValidityState(true);
        console.log("Fallback: validity state set");
      } catch (fallbackError) {
        console.error("Fallback failed:", fallbackError);
      }
    });
  }, []);

  return (
    <div style={{ 
      padding: "20px", 
      maxWidth: "600px", 
      margin: "0 auto",
      minHeight: "100vh",
      display: "flex",
      flexDirection: "column",
      justifyContent: "center"
    }}>
      <Card>
        <CardHeader
          header={
            <Text as="h1" size={600} weight="semibold">
              Support Agents Configuration
            </Text>
          }
        />
        <div style={{ padding: "16px" }}>
          <Body1 style={{ marginBottom: "16px" }}>
            Configure the Support Agents tab for your team. This tab provides a comprehensive 
            support ticket management system with real-time chat capabilities.
          </Body1>
          
          <Text as="h3" size={400} weight="semibold" style={{ marginBottom: "12px" }}>
            Features included:
          </Text>
          
          <ul style={{ listStyle: "none", padding: 0, margin: "16px 0" }}>
            <li style={{ padding: "4px 0", display: "flex", alignItems: "center" }}>
              <span style={{ color: "#16c60c", marginRight: "8px" }}>✓</span>
              <Caption1>Real-time customer support chat</Caption1>
            </li>
            <li style={{ padding: "4px 0", display: "flex", alignItems: "center" }}>
              <span style={{ color: "#16c60c", marginRight: "8px" }}>✓</span>
              <Caption1>Thread management and organization</Caption1>
            </li>
            <li style={{ padding: "4px 0", display: "flex", alignItems: "center" }}>
              <span style={{ color: "#16c60c", marginRight: "8px" }}>✓</span>
              <Caption1>Support ticket tracking</Caption1>
            </li>
            <li style={{ padding: "4px 0", display: "flex", alignItems: "center" }}>
              <span style={{ color: "#16c60c", marginRight: "8px" }}>✓</span>
              <Caption1>Agent collaboration tools</Caption1>
            </li>
            <li style={{ padding: "4px 0", display: "flex", alignItems: "center" }}>
              <span style={{ color: "#16c60c", marginRight: "8px" }}>✓</span>
              <Caption1>Dark/Light theme support</Caption1>
            </li>
          </ul>
          
          <div style={{ 
            backgroundColor: "#f3f2f1", 
            padding: "16px", 
            borderRadius: "6px",
            marginTop: "20px",
            border: "1px solid #e1dfdd"
          }}>
            <Text weight="semibold" style={{ display: "block", marginBottom: "8px" }}>
              Ready to install
            </Text>
            <Caption1>
              Click "Save" to add the Support Agents tab to your team channel. 
              The tab will be configured automatically with all necessary features.
            </Caption1>
          </div>
        </div>
      </Card>
    </div>
  );
};

export default Config;
