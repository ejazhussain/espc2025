import { useState, useEffect } from "react";
import {
  Image,
  TabList,
  Tab,
  SelectTabEvent,
  SelectTabData,
  TabValue,
} from "@fluentui/react-components";
import "./Welcome.css";
import { EditCode } from "./EditCode";
import { CurrentUser } from "./CurrentUser";
import { Deploy } from "./Deploy";
import { Publish } from "./Publish";
import { app } from "@microsoft/teams-js";
import { useData } from "../../lib/useData";
import { NestedAppAuth } from "./NestedAppAuth";
import { TeamsUserCredential, TeamsUserCredentialAuthConfig } from "@microsoft/teamsfx";
import config from "../../lib/config";

export function Welcome(props: { environment?: string }) {
  // Log clientId and tenantId from config
  console.log("Client ID:", config.clientId);
  console.log("Tenant ID:", config.tenantId);

  const { environment } = {
    environment: window.location.hostname === "localhost" ? "local" : "azure",
    ...props,
  };
  const friendlyEnvironmentName =
    {
      local: "local environment",
      azure: "Azure environment",
    }[environment] || "local environment";

  const [selectedValue, setSelectedValue] = useState<TabValue>("local");
  const [userInfo, setUserInfo] = useState<{ displayName: string; email: string } | null>(null);

  // Get user info using TeamsUserCredential
  useEffect(() => {
    const getUserInfo = async () => {
      try {
        const authConfig: TeamsUserCredentialAuthConfig = {
          clientId: config.clientId,
          initiateLoginEndpoint: config.initiateLoginEndpoint,
        };

        const credential = new TeamsUserCredential(authConfig);
        const user = await credential.getUserInfo();
        
        console.log("User info:", user);
        setUserInfo({
          displayName: user.displayName || "",
          email: user.preferredUserName || ""
        });
      } catch (error) {
        console.error("Error getting user info:", error);
      }
    };

    getUserInfo();
  }, []);

  const onTabSelect = (event: SelectTabEvent, data: SelectTabData) => {
    setSelectedValue(data.value);
  };
  const { loading, data, error } = useData(async () => {
    await app.initialize();
    const context = await app.getContext();
    if (context.user) {
      return {
        displayName: context.user.displayName || "",
      };
    }
  });
  const userName = loading || error ? "" : data!.displayName;
  const hubName = useData(async () => {
    await app.initialize();
    const context = await app.getContext();
    return context.app.host.name;
  })?.data;
  
  
  return (
    <div className="welcome page">
      <div className="narrow page-padding">
        <Image src="hello.png" />
        <h1 className="center">Congratulations{userName ? ", " + userName : ""}!</h1>
        {hubName && <p className="center">Your app is running in {hubName}</p>}
        <p className="center">Your app is running in your {friendlyEnvironmentName}</p>
        
        {userInfo && (
          <div className="center" style={{ margin: "20px 0", padding: "10px", border: "1px solid #ccc", borderRadius: "5px" }}>
            <h3>User Information (from TeamsUserCredential):</h3>
            <p><strong>Display Name:</strong> {userInfo.displayName}</p>
            <p><strong>Email Address:</strong> {userInfo.email}</p>
          </div>
        )}

        <div className="tabList">
          <TabList selectedValue={selectedValue} onTabSelect={onTabSelect}>
            <Tab id="Local" value="local">
              1. Build your app locally
            </Tab>
            <Tab id="Azure" value="azure">
              2. Provision and Deploy to the Cloud
            </Tab>
            <Tab id="Publish" value="publish">
              3. Publish to Teams
            </Tab>
          </TabList>
          <div>
            {selectedValue === "local" && (
              <div>
                <EditCode />
                <CurrentUser userName={userName} />
                <NestedAppAuth />
              </div>
            )}
            {selectedValue === "azure" && (
              <div>
                <Deploy />
              </div>
            )}
            {selectedValue === "publish" && (
              <div>
                <Publish />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
