import { useContext } from "react";
import { TeamsFxContext } from "./Context";
import { AgentScreen } from "./chat/AgentScreen";
import { useTeams } from "../lib/useTeams";
export default function Tab() {
  const { themeString } = useContext(TeamsFxContext);
   const [{ context }] = useTeams();

    // subEntityId will be available in context if opened via deep link
  const threadId = context?.page.subPageId;

  // You can log or use threadId as needed
  if (threadId) {
    console.log("ThreadId from Teams context:", threadId);
  }
  return (
    <div
      className={themeString === "default" ? "light" : themeString === "dark" ? "dark" : "contrast"}
    >
  <AgentScreen initialThreadId={threadId} />
    </div>
  );
}
