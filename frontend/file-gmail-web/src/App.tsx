import { useState } from "react";
import { AccessKeyForm } from "./components/AccessKeyForm";
import { MainPage } from "./pages/MainPage";

const ACCESS_DONE_KEY = "filegmail.accessDone";

export default function App() {
  const [accessDone, setAccessDone] = useState(
    () => sessionStorage.getItem(ACCESS_DONE_KEY) === "1",
  );

  const handleAccessSuccess = () => {
    sessionStorage.setItem(ACCESS_DONE_KEY, "1");
    setAccessDone(true);
  };

  if (!accessDone) {
    return <AccessKeyForm onSuccess={handleAccessSuccess} />;
  }

  return <MainPage />;
}