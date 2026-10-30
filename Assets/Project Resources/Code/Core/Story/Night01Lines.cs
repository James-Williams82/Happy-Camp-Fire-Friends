using System.Collections.Generic;

namespace CampfireFriends.Story
{
    /// <summary>Marty Bixby's radio lines for Night 1 (design doc v0.1), shown as text on the walkie.</summary>
    public static class Night01Lines
    {
        public const string Speaker = "BIXBY";

        public static readonly IReadOnlyDictionary<int, string> Text = new Dictionary<int, string>
        {
            [1] = "Testing. Testing. Hello? Ah, there we go. Welcome to Camp Ashwood, the most expensive mistake I ever bought at a discount.",
            [2] = "I'm Marty Bixby. I own this charming pile of lumber, and for tonight, you're my highly trained security division. Don't let that title go to your heads.",
            [3] = "Here's the business model: I tear it down, somebody builds something profitable, and in the meantime you keep the local treasure hunters from walking off with the doorknobs.",
            [4] = "Yep. Demolition. The land's worth more than the buildings, which is a sentence I should've checked before I signed anything.",
            [5] = "That's your office. Fancy, right? Comes with four walls and absolutely no heating bill. Do not switch on the heater; the electrician says it builds character by catching fire.",
            [6] = "Take a flashlight. Take the map. There's a generator behind the maintenance shed. Get that working and the security-side lights should come on. I emphasize should.",
            [7] = "One other thing. People ask about the place's history. There was some ugly business a few years back. That's why it's empty. You're not here to write a documentary. You're here to keep people from stealing copper.",
            [8] = "I bought a deed, not a scrapbook. Focus on the buildings that are still standing.",
            [9] = "Okay, easy job. Find the fuse. Feed that antique dinosaur some fuel. Pull the starter. Then switch on the breaker marked SECURITY. In that order. I paid good money to have those labels put on.",
            [10] = "Cabinet, left side of the shed. Little fuse box. If it's still there, congratulations, we've beaten the scavengers.",
            [11] = "Something fell? In a fifty-year-old shed? Call the newspapers. Check no one got hurt and keep going.",
            [12] = "Hey! Listen to that! She lives. The contractor told me that thing was scrap. Remind me to argue about his invoice.",
            [13] = "There. Lights. See? Already making the place look less like a crime scene. Well, marginally.",
            [14] = "Cabin Six. My favorite. Only one that came with the door pre-removed. Some idiot's been prying it open for souvenirs.",
            [15] = "There's scrap lumber in the rack by the path. Fix that opening so someone can't just waltz inside. Make it ugly. We're tearing it down anyway.",
            [16] = "Look at that, actual craftsmanship. You keep this up, I'll have you build me a house. On your own time, obviously.",
            [17] = "That'll hold. Or at least it'll slow down anybody who doesn't own a crowbar.",
            [18] = "Whoa. Are we securing a cabin or building a bank vault? Easy on my lumber, Rockefeller.",
            [19] = "Almost done. Check the dining-hall latch, then circle back past the recreation court. If somebody's been in there, make a note. Don't go playing detective.",
            [20] = "Oh, the court lights work? Great. Maybe I can sell those separately.",
            [21] = "Sure, take a break. I'm only paying for a security shift, not Olympic training. Keep an eye on the fence while you're at it.",
            [22] = "Yeah, exactly why I hired you. Folks want a little piece of spooky-camp history. I'll settle for them not stealing the plumbing.",
            [23] = "You saw somebody? No? Then write down 'noise,' not 'intruder.' Big difference on an insurance form.",
            [24] = "Something knocking? Probably boards settling. Which cabin?",
            [25] = "...Six?  ...Okay. You secured that one, right?",
            [26] = "Good. Then don't take it apart just to satisfy your curiosity. Head back to the office.",
            [27] = "Nope. Don't touch it. Back to the office. Now.",
            [28] = "I'm serious. We can send somebody in daylight. I am not explaining an injury to an attorney tonight.",
            [29] = "Everybody back? Good. Now fill out the incident line. Properly. If that building collapses, I'd like the paperwork to say it was already thinking about it.",
            [30] = "That's the sensible answer. Old buildings make old-building noises.",
            [31] = "Fine. I'll have somebody check the locks tomorrow. In daylight. With a hard hat.",
            [32] = "'Unknown.' Beautiful. Very reassuring. You really know how to make an owner feel great about his investment.",
            [33] = "You did the job. Don't go back out there tonight. I'll call you in the morning.",
        };

        /// <summary>
        /// The design doc's label for a line, used to name its recording: "01"–"29", then the three
        /// answers to the shift report "30a", "30b", "30c", and the sign-off "31".
        /// </summary>
        public static string DocLabel(int id)
        {
            switch (id)
            {
                case 30: return "30a";
                case 31: return "30b";
                case 32: return "30c";
                case 33: return "31";
                default: return id.ToString("00");
            }
        }

        /// <summary>Seconds to leave a line up: a comfortable reading pace, never under four seconds.</summary>
        public static float ReadingSeconds(int id) =>
            Text.TryGetValue(id, out var text) ? System.Math.Max(4f, text.Split(' ').Length / 2.6f) : 4f;
    }
}
