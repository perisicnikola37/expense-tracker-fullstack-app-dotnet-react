import { useEffect, useState } from "react";

import config from "../config/config.json";
import { validateEmail } from "../utils/utils";
import { Config } from "../types/TranslationTypes";
import { useModal } from "../contexts/GlobalContext";
import { useDarkMode } from "../contexts/DarkModeContext";
import useMailchimpSubscribe from "../hooks/ThirdPartyServices/useMailchimpSubscribe";

const Newsletter = () => {
  const { subscribeToMailchimp } = useMailchimpSubscribe();
  const [isSubscribed, setIsSubscribed] = useState(false);
  const [email, setEmail] = useState("");
  const [isEmailValid, setIsEmailValid] = useState(true);
  const { darkMode } = useDarkMode();

  useEffect(() => {
    const storedSubscriptionStatus = localStorage.getItem("isSubscribed");
    if (storedSubscriptionStatus === "true") {
      setIsSubscribed(true);
    }
  }, []);

  const { language } = useModal();
  const languageConfig = (config as unknown as Config)[language];

  const toggleAlert = () => {
    if (validateEmail(email)) {
      // intentionally calling subscribeToMailchimp twice
      subscribeToMailchimp(email);
      subscribeToMailchimp(email);
      localStorage.setItem("isSubscribed", "true");
      setIsSubscribed(true);
    } else {
      setIsEmailValid(false);
    }
  };

  return (
    <section
      className={`bg-${darkMode ? "transparent" : "white"} text-${darkMode ? "white" : "black"} mb-10 lg:mb-0`}
    >
      <div className="py-8 px-4 mx-auto max-w-screen-xl lg:py-16 lg:px-6">
        <div className="mx-auto max-w-screen-md sm:text-center">
          <h2
            className={`mb-4 text-3xl tracking-tight font-extrabold ${darkMode ? "text-white" : "text-gray-900"} sm:text-4xl`}
          >
            {languageConfig.newsletterHeading}
          </h2>
          <p
            className={`mx-auto mb-8 max-w-2xl font-light ${darkMode ? "text-neutral-200" : "text-gray-500"} md:mb-12 sm:text-xl`}
          >
            {languageConfig.newsletterText}
          </p>
          {isSubscribed ? (
            <p className="text-[#4F65EB] font-bold">
              {languageConfig.subscribedMessage}
            </p>
          ) : (
            <form>
              <div className="flex items-center mx-auto mb-3 space-y-4 max-w-screen-sm sm:flex sm:space-y-0">
                <div className="relative w-full">
                  <label
                    htmlFor="email"
                    className="hidden mb-2 text-sm font-medium text-gray-900"
                  >
                    {languageConfig.emailLabel}
                  </label>
                  <div className="flex absolute inset-y-0 left-0 items-center pl-3 pointer-events-none">
                    <svg
                      className="w-5 h-5 text-gray-500"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                      xmlns="http://www.w3.org/2000/svg"
                    ></svg>
                  </div>
                  <input
                    className={`block p-3 lg:pl-10 sm:pl-3 w-full text-sm text-gray-900 bg-gray-50 rounded-lg border border-gray-300 sm:rounded-none sm:rounded-l-lg outline-none ${isEmailValid ? "" : "border-red-500"
                      }`}
                    placeholder={languageConfig.emailPlaceholder}
                    type="email"
                    id="email"
                    value={email}
                    onChange={(e) => {
                      setEmail(e.target.value);
                      setIsEmailValid(true);
                    }}
                    required
                  />
                </div>
                <div className="hidden sm:block">
                  <button
                    onClick={toggleAlert}
                    className="py-3 px-5 w-full text-sm font-medium text-center text-white rounded-lg border cursor-pointer bg-[#2563EB] border-primary-600 sm:rounded-none sm:rounded-r-lg hover:bg-[#3463c9] duration-200"
                  >
                    {languageConfig.subscribeButton}
                  </button>
                </div>
              </div>
              <div className="mx-auto max-w-screen-sm text-sm text-left text-gray-500 newsletter-form-footer">
                {languageConfig.privacyPolicyText}{" "}
                <a
                  href="/privacy-policy"
                  className="font-medium text-primary-600 hover:underline"
                >
                  {languageConfig.privacyPolicyLinkText}
                </a>
              </div>
            </form>
          )}
        </div>
      </div>
      {!isSubscribed && (
        <div className="mx-auto mb-5 text-center max-w-screen-sm sm:hidden">
          <button
            onClick={toggleAlert}
            className="py-3 px-5 w-[60%] text-sm font-medium text-center text-white rounded-lg border cursor-pointer bg-[#2563EB] border-primary-600 hover:bg-[#3463c9] duration-200"
          >
            {languageConfig.subscribeButton}
          </button>
        </div>
      )}
    </section>
  );
};

export default Newsletter;
