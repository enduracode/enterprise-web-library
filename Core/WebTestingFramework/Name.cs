﻿using EnterpriseWebLibrary.TewlContrib;

namespace EnterpriseWebLibrary.WebTestingFramework {
	/// <summary>
	/// A person's name.
	/// </summary>
	public class Name {
		#region data

		private static readonly Name[] rawNames =
			{
				new Name( "Earnestine", "Remaley" ), new Name( "Milagros", "Buttars" ), new Name( "Kurt", "Hannaford" ), new Name( "Erik", "Basler" ),
				new Name( "Julio", "Ciccone" ), new Name( "Jamie", "Lalli" ), new Name( "Darren", "Croucher" ), new Name( "Kelly", "Heft" ),
				new Name( "Tyrone", "Warkentin" ), new Name( "Lance", "Gatica" ), new Name( "Jami", "Blish" ), new Name( "Nelson", "Bladen" ),
				new Name( "Jamie", "Rikard" ), new Name( "Kelly", "Shover" ), new Name( "Allan", "Marney" ), new Name( "Tia", "Brindley" ),
				new Name( "Tameka", "Shead" ), new Name( "Rosalinda", "Bullion" ), new Name( "Cody", "Lindow" ), new Name( "Max", "Rohrbaugh" ),
				new Name( "Fernando", "Pospisil" ), new Name( "Julio", "Tindle" ), new Name( "Cody", "Rumbaugh" ), new Name( "Amie", "Votaw" ),
				new Name( "Roslyn", "Cervone" ), new Name( "Jessie", "Balsamo" ), new Name( "Margery", "Lawry" ), new Name( "Lonnie", "Onstad" ),
				new Name( "Neil", "Shahid" ), new Name( "Nita", "Frenette" ), new Name( "Kurt", "Karcher" ), new Name( "Kenya", "Battersby" ),
				new Name( "Tyrone", "Renna" ), new Name( "Darryl", "Vanasse" ), new Name( "Liza", "Petrick" ), new Name( "Darren", "Egner" ),
				new Name( "Kurt", "Hammersley" ), new Name( "Nannie", "Montesinos" ), new Name( "Lonnie", "Pflug" ), new Name( "Sharron", "Reimers" ),
				new Name( "Erik", "Manos" ), new Name( "Ted", "Waren" ), new Name( "Guy", "Clinger" ), new Name( "Avis", "Diers" ), new Name( "Tyrone", "Cissell" ),
				new Name( "Jessie", "Sola" ), new Name( "Javier", "Getchell" ), new Name( "Lenore", "Swaby" ), new Name( "Lorrie", "Juntunen" ),
				new Name( "Malinda", "Cornette" ), new Name( "Earlene", "Ostler" ), new Name( "Zelma", "Stice" ), new Name( "Roxie", "Arrant" ),
				new Name( "Javier", "Gabrielli" ), new Name( "Max", "Mosquera" ), new Name( "Lonnie", "Stucker" ), new Name( "Margery", "Seamons" ),
				new Name( "Tania", "Salls" ), new Name( "Hugh", "Steimle" ), new Name( "Zelma", "Fane" ), new Name( "Rosalinda", "Ritenour" ),
				new Name( "Kelly", "Mcguigan" ), new Name( "Ericka", "Meshell" ), new Name( "Javier", "Raatz" ), new Name( "Esmeralda", "Grubaugh" ),
				new Name( "Melisa", "Zheng" ), new Name( "Alejandra", "Vibbert" ), new Name( "Julio", "Locher" ), new Name( "Lakisha", "Bruck" ),
				new Name( "Selena", "Liakos" ), new Name( "Jessie", "Pieroni" ), new Name( "Malinda", "Mingus" ), new Name( "Alana", "Belvin" ),
				new Name( "Nelson", "Goza" ), new Name( "Sofia", "Dobos" ), new Name( "Penelope", "Hardt" ), new Name( "Serena", "Marnell" ),
				new Name( "Dollie", "Jiron" ), new Name( "Rosalinda", "Stoudemire" ), new Name( "Lance", "Hazan" ), new Name( "Eve", "Lanahan" ),
				new Name( "Nita", "Farraj" ), new Name( "Mathew", "Tomasini" ), new Name( "Tanisha", "Shima" ), new Name( "Kurt", "Kotch" ),
				new Name( "Benita", "Holmer" ), new Name( "Jessie", "Becerril" ), new Name( "Nannie", "Krout" ), new Name( "Roslyn", "Bullen" ),
				new Name( "Erik", "Sol" ), new Name( "Ted", "Lasko" ), new Name( "Erik", "Veras" ), new Name( "Jamie", "Powelson" ), new Name( "Cody", "Hollabaugh" ),
				new Name( "Earlene", "Cilley" ), new Name( "Nannie", "Rommel" ), new Name( "Clayton", "Sedberry" ), new Name( "Mathew", "Florian" ),
				new Name( "Clayton", "Hardage" ), new Name( "Clayton", "Constance" ), new Name( "Kenya", "Keogh" ), new Name( "Kelly", "Jarrells" ),
				new Name( "Elnora", "Tuner" ), new Name( "Ashlee", "Speth" ), new Name( "Julianne", "Montealegre" ), new Name( "Darryl", "Scardina" ),
				new Name( "Malinda", "Wendler" ), new Name( "Carlene", "Glatt" ), new Name( "Esmeralda", "Degroot" ), new Name( "Penelope", "Nilles" ),
				new Name( "Tia", "Cullins" ), new Name( "Amie", "Castrejon" ), new Name( "Noreen", "Jefcoat" ), new Name( "Nannie", "Mcalexander" ),
				new Name( "Lance", "Waguespack" ), new Name( "Hugh", "Kilcrease" ), new Name( "Malinda", "Lenihan" ), new Name( "Javier", "Heilema" ),
				new Name( "Leah", "Abeyta" ), new Name( "Steven", "Abner" ), new Name( "Thomas", "Acton" ), new Name( "Raymond", "Addis" ),
				new Name( "Irma", "Adkins" ), new Name( "Candace", "Adkins" ), new Name( "Kristina", "Adorno" ), new Name( "Nellie", "Aguilar" ),
				new Name( "Crystal", "Ahern" ), new Name( "James", "Aho" ), new Name( "John", "Albanese" ), new Name( "Terry", "Albers" ),
				new Name( "Vanessa", "Albrecht" ), new Name( "Gregory", "Alex" ), new Name( "James", "Alexis" ), new Name( "Jack", "Alford" ),
				new Name( "Jeffrey", "Alicea" ), new Name( "Harold", "Allison" ), new Name( "Nathan", "Allred" ), new Name( "Theresa", "Amaya" ),
				new Name( "Juana", "Amundson" ), new Name( "Daniel", "Amundson" ), new Name( "Leonard", "Anders" ), new Name( "Chris", "Andersen" ),
				new Name( "Paul", "Anson" ), new Name( "Chris", "Applewhite" ), new Name( "Jerry", "Aquilar" ), new Name( "Ralph", "Araujo" ),
				new Name( "Joshua", "Archambault" ), new Name( "Scott", "Ard" ), new Name( "Richard", "Arenas" ), new Name( "Victor", "Arnett" ),
				new Name( "George", "Arnette" ), new Name( "Jodi", "Arredondo" ), new Name( "Brittany", "Arruda" ), new Name( "Stephen", "Arsenault" ),
				new Name( "Joseph", "Asher" ), new Name( "Todd", "Atchison" ), new Name( "Steve", "Atherton" ), new Name( "Matthew", "Atherton" ),
				new Name( "Victor", "Aucoin" ), new Name( "Lynda", "Austin" ), new Name( "Larry", "Avery" ), new Name( "Eileen", "Babbitt" ),
				new Name( "Nora", "Badger" ), new Name( "Julie", "Bagby" ), new Name( "Ernest", "Bandy" ), new Name( "Roy", "Barahona" ),
				new Name( "Earl", "Barajas" ), new Name( "Kelli", "Barker" ), new Name( "Gertrude", "Barnwell" ), new Name( "Roger", "Barrett" ),
				new Name( "Peggy", "Bartlett" ), new Name( "Stella", "Baskerville" ), new Name( "Gerald", "Baskin" ), new Name( "Jose", "Bateman" ),
				new Name( "Andrew", "Bay" ), new Name( "Sally", "Bazan" ), new Name( "Tonya", "Beaudette" ), new Name( "Janie", "Beaumont" ),
				new Name( "Ernest", "Becerra" ), new Name( "Sylvia", "Beckham" ), new Name( "Sharon", "Beckley" ), new Name( "Jill", "Bedard" ),
				new Name( "Vincent", "Bee" ), new Name( "Phillip", "Behling" ), new Name( "Diane", "Behling" ), new Name( "Erma", "Behr" ),
				new Name( "Terri", "Belew" ), new Name( "Denise", "Benedetto" ), new Name( "Raymond", "Benn" ), new Name( "Jason", "Bent" ),
				new Name( "Albert", "Benton" ), new Name( "Edith", "Benton" ), new Name( "James", "Berkley" ), new Name( "Shawn", "Betz" ),
				new Name( "Luis", "Bianchi" ), new Name( "Natasha", "Billups" ), new Name( "Marguerite", "Billups" ), new Name( "Minnie", "Bingham" ),
				new Name( "William", "Binion" ), new Name( "Andrew", "Birchfield" ), new Name( "Cathy", "Bixler" ), new Name( "Clarence", "Black" ),
				new Name( "Frank", "Blackwell" ), new Name( "Jose", "Blackwood" ), new Name( "Joy", "Blakemore" ), new Name( "Christina", "Blalock" ),
				new Name( "Rodney", "Blow" ), new Name( "Michele", "Bock" ), new Name( "Jessica", "Bogart" ), new Name( "Laurie", "Boger" ),
				new Name( "Edward", "Bolding" ), new Name( "Felicia", "Boney" ), new Name( "Louis", "Borst" ), new Name( "Jimmy", "Boswell" ),
				new Name( "Maxine", "Bosworth" ), new Name( "Juan", "Bott" ), new Name( "Sally", "Botts" ), new Name( "Carole", "Bourg" ),
				new Name( "Brandi", "Bowlin" ), new Name( "Earl", "Bowlin" ), new Name( "Sylvia", "Bracey" ), new Name( "John", "Brady" ),
				new Name( "Roy", "Braley" ), new Name( "Luis", "Brandenburg" ), new Name( "Harry", "Bravo" ), new Name( "Jeannette", "Brawner" ),
				new Name( "Nicholas", "Brenner" ), new Name( "Sonya", "Bridge" ), new Name( "Adam", "Bridgeman" ), new Name( "Belinda", "Brink" ),
				new Name( "Allison", "Brinson" ), new Name( "Sean", "Broaddus" ), new Name( "Jeanette", "Brockington" ), new Name( "Kenneth", "Brooker" ),
				new Name( "Mildred", "Brunner" ), new Name( "Jimmy", "Buggs" ), new Name( "Vincent", "Bunker" ), new Name( "Patrick", "Burg" ),
				new Name( "Travis", "Burgos" ), new Name( "Paul", "Burkhalter" ), new Name( "Julia", "Burress" ), new Name( "Charles", "Burris" ),
				new Name( "Natalie", "Burt" ), new Name( "Miriam", "Bush" ), new Name( "Anna", "Bushnell" ), new Name( "Rachel", "Busse" ),
				new Name( "Bobby", "Button" ), new Name( "Genevieve", "Byrne" ), new Name( "Albert", "Cabral" ), new Name( "Wayne", "Cadena" ),
				new Name( "Sarah", "Cadena" ), new Name( "Louis", "Cage" ), new Name( "Samuel", "Camacho" ), new Name( "Erika", "Cancel" ),
				new Name( "Jimmy", "Canterbury" ), new Name( "Christopher", "Cantu" ), new Name( "Mae", "Caraballo" ), new Name( "Louis", "Carlson" ),
				new Name( "Roy", "Carnahan" ), new Name( "Mildred", "Carney" ), new Name( "Andrew", "Carrillo" ), new Name( "Sonya", "Carrington" ),
				new Name( "Martin", "Carrol" ), new Name( "Joseph", "Carrol" ), new Name( "Geneva", "Cary" ), new Name( "Lawrence", "Caston" ),
				new Name( "Curtis", "Cathey" ), new Name( "Jack", "Catlin" ), new Name( "Scott", "Caviness" ), new Name( "Eileen", "Chacon" ),
				new Name( "Sally", "Chaisson" ), new Name( "Gwendolyn", "Chamberlain" ), new Name( "Vicky", "Champlin" ), new Name( "Viola", "Chan" ),
				new Name( "Kristi", "Chappell" ), new Name( "Matthew", "Charette" ), new Name( "Dennis", "Chartier" ), new Name( "Christina", "Chasteen" ),
				new Name( "Jason", "Chesson" ), new Name( "Rosemary", "Childs" ), new Name( "Lawrence", "Christianson" ), new Name( "Benjamin", "Clair" ),
				new Name( "Shawn", "Clare" ), new Name( "Douglas", "Clarke" ), new Name( "Mark", "Clemente" ), new Name( "Rosa", "Clift" ),
				new Name( "Paula", "Clift" ), new Name( "Roger", "Cloninger" ), new Name( "Michael", "Coakley" ), new Name( "Mark", "Coburn" ),
				new Name( "Daniel", "Coffey" ), new Name( "Steve", "Cofield" ), new Name( "Beverly", "Cole" ), new Name( "Cathy", "Collett" ),
				new Name( "Marvin", "Collinsworth" ), new Name( "Pam", "Collinsworth" ), new Name( "Richard", "Colon" ), new Name( "Vera", "Compton" ),
				new Name( "Juan", "Conant" ), new Name( "Charles", "Connelly" ), new Name( "Wendy", "Constant" ), new Name( "Billy", "Corder" ),
				new Name( "Lena", "Corona" ), new Name( "Bruce", "Coronado" ), new Name( "Debbie", "Coronel" ), new Name( "Edward", "Cottingham" ),
				new Name( "Carl", "Coughlin" ), new Name( "Adrienne", "Countryman" ), new Name( "Adam", "Coyne" ), new Name( "Ruby", "Cozart" ),
				new Name( "Roger", "Crespo" ), new Name( "Dianne", "Crisp" ), new Name( "Harold", "Criss" ), new Name( "Isabel", "Culbreth" ),
				new Name( "Alan", "Curry" ), new Name( "Kenneth", "Curtiss" ), new Name( "Heather", "Cusick" ), new Name( "Cathy", "Cusick" ),
				new Name( "Ralph", "Dacosta" ), new Name( "William", "Dacosta" ), new Name( "Jeffery", "Dale" ), new Name( "Rosemary", "Daley" ),
				new Name( "Anna", "Dalrymple" ), new Name( "Norma", "Dalrymple" ), new Name( "Jack", "Dameron" ), new Name( "Roy", "Davies" ),
				new Name( "Ryan", "Davis" ), new Name( "Suzanne", "Davisson" ), new Name( "Stephen", "Dawson" ), new Name( "Marvin", "Day" ),
				new Name( "Kevin", "Dearborn" ), new Name( "Craig", "Delafuente" ), new Name( "Carlos", "Delagarza" ), new Name( "Randy", "Deltoro" ),
				new Name( "David", "Dement" ), new Name( "Cynthia", "Deming" ), new Name( "Nancy", "Dendy" ), new Name( "Ruby", "Dennard" ),
				new Name( "Frank", "Dennard" ), new Name( "Keith", "Denny" ), new Name( "Christine", "Depriest" ), new Name( "Jimmy", "Derosier" ),
				new Name( "Fannie", "Devaughn" ), new Name( "Jerry", "Devoe" ), new Name( "Anthony", "Deyoung" ), new Name( "Raymond", "Dickey" ),
				new Name( "John", "Dietz" ), new Name( "Jacqueline", "Dillard" ), new Name( "Kenneth", "Dingman" ), new Name( "Lula", "Dinh" ),
				new Name( "Martin", "Dionne" ), new Name( "Jacquelyn", "Dixson" ), new Name( "Joshua", "Dixson" ), new Name( "Kristy", "Doak" ),
				new Name( "Deborah", "Dockery" ), new Name( "Brandy", "Dombrowski" ), new Name( "Irma", "Doney" ), new Name( "Tony", "Donnelly" ),
				new Name( "Melissa", "Donofrio" ), new Name( "Belinda", "Dotson" ), new Name( "Alison", "Dove" ), new Name( "Shelia", "Doyle" ),
				new Name( "Lucille", "Drew" ), new Name( "Audrey", "Duck" ), new Name( "Toni", "Duda" ), new Name( "Ella", "Duggan" ), new Name( "Timothy", "Duke" ),
				new Name( "Katherine", "Dull" ), new Name( "Billy", "Dunlap" ), new Name( "Tanya", "Duong" ), new Name( "Michele", "Duplessis" ),
				new Name( "Robert", "Duplessis" ), new Name( "Jason", "Duplessis" ), new Name( "Velma", "Durkin" ), new Name( "William", "Durkin" ),
				new Name( "Loretta", "Dyer" ), new Name( "Billy", "Eady" ), new Name( "Kevin", "Eames" ), new Name( "Arthur", "Earle" ), new Name( "Tony", "East" ),
				new Name( "Gregory", "Easterling" ), new Name( "Gary", "Eberle" ), new Name( "David", "Eberly" ), new Name( "William", "Eberly" ),
				new Name( "Fred", "Ebersole" ), new Name( "Maggie", "Echevarria" ), new Name( "Randy", "Edgerton" ), new Name( "Mamie", "Eichelberger" ),
				new Name( "Brian", "Eller" ), new Name( "Chris", "Ellingson" ), new Name( "Steve", "Ellison" ), new Name( "Kevin", "Ellison" ),
				new Name( "Roxanne", "Elwell" ), new Name( "Julie", "Engelhardt" ), new Name( "Albert", "England" ), new Name( "Matthew", "Espinal" ),
				new Name( "Maryann", "Essex" ), new Name( "Ethel", "Estabrook" ), new Name( "Mamie", "Fagan" ), new Name( "Vera", "Fahey" ),
				new Name( "Sandra", "Fair" ), new Name( "Megan", "Fannin" ), new Name( "Manuel", "Farnsworth" ), new Name( "Bobby", "Farrell" ),
				new Name( "Lynda", "Farrow" ), new Name( "Jessica", "Faust" ), new Name( "Jeffrey", "Felder" ), new Name( "Edward", "Fellows" ),
				new Name( "Todd", "Ferrell" ), new Name( "Bonnie", "Fielder" ), new Name( "Joshua", "Fielder" ), new Name( "Donald", "Fields" ),
				new Name( "Nicholas", "Fife" ), new Name( "Charles", "Findlay" ), new Name( "Audrey", "Fitzgerald" ), new Name( "Dawn", "Fitzpatrick" ),
				new Name( "Terry", "Flanders" ), new Name( "Russell", "Flemming" ), new Name( "Martha", "Fonseca" ), new Name( "Yvonne", "Ford" ),
				new Name( "Jacquelyn", "Forde" ), new Name( "Richard", "Foreman" ), new Name( "Deanna", "Forsyth" ), new Name( "George", "Fortenberry" ),
				new Name( "Ralph", "Frazee" ), new Name( "Shawn", "Frederick" ), new Name( "Vincent", "Fredericks" ), new Name( "Paula", "Freeze" ),
				new Name( "Douglas", "Friend" ), new Name( "Maxine", "Fults" ), new Name( "Sonya", "Fuqua" ), new Name( "Douglas", "Gabbard" ),
				new Name( "Carl", "Gadson" ), new Name( "Nina", "Gage" ), new Name( "Travis", "Gale" ), new Name( "Kristina", "Galloway" ),
				new Name( "Amelia", "Garay" ), new Name( "Maureen", "Garner" ), new Name( "Janie", "Garris" ), new Name( "Jeffrey", "Gartner" ),
				new Name( "Lawrence", "Gaylor" ), new Name( "Judy", "Geisler" ), new Name( "Eric", "Gentry" ), new Name( "Marvin", "Gerber" ),
				new Name( "Sarah", "Gilley" ), new Name( "Donald", "Giordano" ), new Name( "Robert", "Gish" ), new Name( "Carlos", "Goldstein" ),
				new Name( "Beth", "Gooch" ), new Name( "Phillip", "Goodnight" ), new Name( "Patrick", "Gottlieb" ), new Name( "Brian", "Gourley" ),
				new Name( "Arthur", "Grace" ), new Name( "Anne", "Grasso" ), new Name( "James", "Gravely" ), new Name( "Michael", "Graziano" ),
				new Name( "Norman", "Greenleaf" ), new Name( "Eric", "Greenwood" ), new Name( "Rosemary", "Greiner" ), new Name( "Jesse", "Grenier" ),
				new Name( "Walter", "Grier" ), new Name( "Bessie", "Grizzle" ), new Name( "Clarence", "Groves" ), new Name( "Louis", "Grubb" ),
				new Name( "Joanna", "Guidry" ), new Name( "Marilyn", "Gulick" ), new Name( "Louis", "Gulledge" ), new Name( "Irma", "Gunderson" ),
				new Name( "Juan", "Gupta" ), new Name( "Tina", "Gustin" ), new Name( "Julia", "Haag" ), new Name( "Norman", "Hackney" ),
				new Name( "Louis", "Haddad" ), new Name( "Allen", "Hafer" ), new Name( "Norman", "Hage" ), new Name( "Candace", "Hailey" ),
				new Name( "Walter", "Hailey" ), new Name( "Edward", "Halley" ), new Name( "Frank", "Hallock" ), new Name( "Eric", "Hallowell" ),
				new Name( "Stephen", "Hammack" ), new Name( "Arlene", "Hardiman" ), new Name( "David", "Harkey" ), new Name( "Terri", "Harlan" ),
				new Name( "Sabrina", "Harlan" ), new Name( "Brittany", "Harlow" ), new Name( "William", "Harney" ), new Name( "Bridget", "Harrod" ),
				new Name( "Sheri", "Hart" ), new Name( "Kara", "Hartford" ), new Name( "Stephen", "Hartwig" ), new Name( "Juan", "Hasson" ),
				new Name( "Lawrence", "Haug" ), new Name( "Alberta", "Haug" ), new Name( "Terry", "Hawkins" ), new Name( "Glenn", "Haygood" ),
				new Name( "Billy", "Hazelwood" ), new Name( "Jo", "Headrick" ), new Name( "Lynne", "Hecht" ), new Name( "Douglas", "Heckman" ),
				new Name( "Tara", "Hedgepeth" ), new Name( "Keith", "Heller" ), new Name( "Edward", "Heller" ), new Name( "Martin", "Hendrick" ),
				new Name( "Roger", "Henriquez" ), new Name( "Sheryl", "Herbert" ), new Name( "Stanley", "Herndon" ), new Name( "Shelia", "Hershberger" ),
				new Name( "Victoria", "Hewitt" ), new Name( "Bryan", "Hibbs" ), new Name( "Brittany", "Hitchcock" ), new Name( "Kristin", "Hoang" ),
				new Name( "Bruce", "Hobart" ), new Name( "Walter", "Hobson" ), new Name( "Sue", "Hodges" ), new Name( "Lawrence", "Holladay" ),
				new Name( "Jason", "Holladay" ), new Name( "Joseph", "Hollar" ), new Name( "Peter", "Hollingshead" ), new Name( "Marsha", "Hollister" ),
				new Name( "Carlos", "Holloman" ), new Name( "Yolanda", "Holman" ), new Name( "Scott", "Holmes" ), new Name( "Jacquelyn", "Holmquist" ),
				new Name( "Erma", "Holst" ), new Name( "Inez", "Hoopes" ), new Name( "Scott", "Hoopes" ), new Name( "Jose", "Hopkins" ),
				new Name( "Joshua", "Hornback" ), new Name( "Amanda", "Hostetler" ), new Name( "Janet", "Houk" ), new Name( "Norma", "House" ),
				new Name( "Erica", "Houser" ), new Name( "Wendy", "Howard" ), new Name( "Latoya", "Hsu" ), new Name( "Kenneth", "Hubbell" ),
				new Name( "Alberta", "Hubbs" ), new Name( "Diane", "Hudgins" ), new Name( "Jesse", "Huerta" ), new Name( "Steven", "Humbert" ),
				new Name( "Nicholas", "Hurd" ), new Name( "Harry", "Hutson" ), new Name( "Christina", "Huynh" ), new Name( "Hannah", "Hyder" ),
				new Name( "Gerald", "Irvin" ), new Name( "Marvin", "Irving" ), new Name( "Bryan", "Isom" ), new Name( "Jeffery", "Ives" ),
				new Name( "Ralph", "Jacobsen" ), new Name( "Roberta", "Jaeger" ), new Name( "Veronica", "James" ), new Name( "Antoinette", "Jamison" ),
				new Name( "Carla", "Jaquez" ), new Name( "Tony", "Jauregui" ), new Name( "Lena", "Jeffcoat" ), new Name( "Maureen", "Jennings" ),
				new Name( "Clara", "Jewett" ), new Name( "Tony", "Jimenez" ), new Name( "Russell", "Jobe" ), new Name( "Priscilla", "Johanson" ),
				new Name( "Virginia", "Jude" ), new Name( "Keith", "Kahler" ), new Name( "Justin", "Kapp" ), new Name( "Wayne", "Kasten" ),
				new Name( "Larry", "Katz" ), new Name( "Jeffery", "Kautz" ), new Name( "Vincent", "Kay" ), new Name( "Luis", "Kearse" ),
				new Name( "Sandra", "Kearse" ), new Name( "Brandon", "Keiser" ), new Name( "William", "Kellar" ), new Name( "Charles", "Kelton" ),
				new Name( "Dawn", "Kersey" ), new Name( "Maggie", "Kibler" ), new Name( "Michael", "Kidder" ), new Name( "Ronald", "Kim" ),
				new Name( "Randy", "Kim" ), new Name( "Mike", "Kimbrell" ), new Name( "Jacquelyn", "Kingston" ), new Name( "Bessie", "Kingston" ),
				new Name( "Toni", "Kinney" ), new Name( "Eugene", "Kittle" ), new Name( "Teresa", "Klein" ), new Name( "Dennis", "Kling" ),
				new Name( "Jane", "Knighton" ), new Name( "Randy", "Knisley" ), new Name( "Manuel", "Knoll" ), new Name( "Sheila", "Kohn" ),
				new Name( "Steve", "Kohn" ), new Name( "Yvette", "Kroeger" ), new Name( "Tiffany", "Kropp" ), new Name( "Diana", "Kuntz" ),
				new Name( "Sharon", "Labonte" ), new Name( "Carl", "Ladner" ), new Name( "Carla", "Lafontaine" ), new Name( "Maryann", "Lail" ),
				new Name( "Emma", "Lammers" ), new Name( "Craig", "Lamontagne" ), new Name( "Willie", "Lamothe" ), new Name( "Curtis", "Landon" ),
				new Name( "Renee", "Lassiter" ), new Name( "Robert", "Latour" ), new Name( "Howard", "Laughlin" ), new Name( "Jason", "Lawrence" ),
				new Name( "Edward", "Lawton" ), new Name( "Brandon", "Lawton" ), new Name( "Eugene", "Layman" ), new Name( "Loretta", "Leatherwood" ),
				new Name( "Jacqueline", "Leclair" ), new Name( "Raymond", "Ledbetter" ), new Name( "Myra", "Leininger" ), new Name( "Krista", "Leiva" ),
				new Name( "Daniel", "Leslie" ), new Name( "Brandon", "Lett" ), new Name( "Virginia", "Levin" ), new Name( "Marlene", "Levy" ),
				new Name( "Stephen", "Lewin" ), new Name( "Cynthia", "Lewin" ), new Name( "Ida", "Ley" ), new Name( "Jeffery", "Li" ), new Name( "Paul", "Lind" ),
				new Name( "Pamela", "Lindholm" ), new Name( "Todd", "Little" ), new Name( "Jeanne", "Lockhart" ), new Name( "Deanna", "Logsdon" ),
				new Name( "Allison", "Lomax" ), new Name( "Patty", "Longoria" ), new Name( "Gertrude", "Lowrey" ), new Name( "Bryan", "Lowrey" ),
				new Name( "Pamela", "Luck" ), new Name( "Carl", "Lundberg" ), new Name( "Lola", "Lunsford" ), new Name( "Adam", "Lupo" ),
				new Name( "Kenneth", "Lussier" ), new Name( "Kevin", "Lutes" ), new Name( "Edna", "Luu" ), new Name( "Bruce", "Lynch" ), new Name( "Craig", "Ma" ),
				new Name( "Hazel", "Maas" ), new Name( "Hilda", "Macias" ), new Name( "Brandon", "Macy" ), new Name( "Curtis", "Maddux" ),
				new Name( "Janie", "Madewell" ), new Name( "Debbie", "Madsen" ), new Name( "Russell", "Magee" ), new Name( "Joy", "Magill" ),
				new Name( "Georgia", "Magill" ), new Name( "Mamie", "Magnuson" ), new Name( "Antonio", "Mahon" ), new Name( "Danny", "Makowski" ),
				new Name( "Sara", "Malinowski" ), new Name( "Ronald", "Mallette" ), new Name( "Jo", "Malloy" ), new Name( "Peter", "Maloney" ),
				new Name( "Sara", "Mangrum" ), new Name( "Marcia", "Manley" ), new Name( "Robert", "Mantooth" ), new Name( "Arlene", "Mapp" ),
				new Name( "Natasha", "Marble" ), new Name( "Kathy", "Marino" ), new Name( "Madeline", "Marler" ), new Name( "Madeline", "Marriott" ),
				new Name( "Richard", "Mars" ), new Name( "Phillip", "Martinelli" ), new Name( "Harry", "Martino" ), new Name( "Howard", "Mascarenas" ),
				new Name( "Kristen", "Massa" ), new Name( "Vicky", "Matheson" ), new Name( "Kristin", "Mathewson" ), new Name( "Vicki", "Maxfield" ),
				new Name( "Amber", "Mayle" ), new Name( "Vera", "Mcardle" ), new Name( "Candice", "Mcatee" ), new Name( "Gloria", "Mcbee" ),
				new Name( "Harry", "Mccallister" ), new Name( "Keith", "Mccarthy" ), new Name( "Jerry", "Mcclellan" ), new Name( "Joann", "Mccloud" ),
				new Name( "Cynthia", "Mccombs" ), new Name( "Danny", "Mccutchen" ), new Name( "Janie", "Mcdowell" ), new Name( "George", "Mceachern" ),
				new Name( "Terry", "Mcelrath" ), new Name( "Sarah", "Mcfall" ), new Name( "Jeffrey", "Mcgaha" ), new Name( "Manuel", "Mchenry" ),
				new Name( "Ramona", "Mcintire" ), new Name( "Jimmy", "Mckelvey" ), new Name( "Dale", "Mckelvey" ), new Name( "Paul", "Mckenney" ),
				new Name( "Ryan", "Mckibben" ), new Name( "Kevin", "Mckown" ), new Name( "Josephine", "Mclain" ), new Name( "Jerry", "Mclellan" ),
				new Name( "Carl", "Mcmillon" ), new Name( "Gwendolyn", "Mcmillon" ), new Name( "Renee", "Mcmullen" ), new Name( "Allen", "Mears" ),
				new Name( "Marianne", "Mears" ), new Name( "Clarence", "Medina" ), new Name( "Emily", "Mefford" ), new Name( "Edward", "Melancon" ),
				new Name( "Melanie", "Menefee" ), new Name( "Jason", "Mercer" ), new Name( "Phillip", "Meredith" ), new Name( "Olga", "Merida" ),
				new Name( "Ronald", "Merida" ), new Name( "Earl", "Michaelson" ), new Name( "Eugene", "Michels" ), new Name( "Norma", "Mick" ),
				new Name( "Darlene", "Mickens" ), new Name( "Scott", "Miguel" ), new Name( "Philip", "Miranda" ), new Name( "Vicki", "Mizell" ),
				new Name( "Judy", "Mizell" ), new Name( "Alma", "Moeller" ), new Name( "William", "Mohan" ), new Name( "Mae", "Monahan" ),
				new Name( "Brian", "Montague" ), new Name( "Hilda", "Monte" ), new Name( "Loretta", "Montelongo" ), new Name( "Brian", "Montez" ),
				new Name( "David", "Moralez" ), new Name( "Elsie", "Morehouse" ), new Name( "Tammy", "Morton" ), new Name( "Kathy", "Moss" ),
				new Name( "Beulah", "Moten" ), new Name( "Dale", "Moxley" ), new Name( "Victor", "Moxley" ), new Name( "Janet", "Mueller" ),
				new Name( "Tony", "Mulholland" ), new Name( "Debra", "Mulligan" ), new Name( "Philip", "Mulligan" ), new Name( "Ruth", "Mullis" ),
				new Name( "Mildred", "Murchison" ), new Name( "Brandon", "Murdoch" ), new Name( "Marvin", "Musgrave" ), new Name( "Philip", "Nagel" ),
				new Name( "Ronald", "Navarro" ), new Name( "Eunice", "Neill" ), new Name( "Travis", "Neilson" ), new Name( "Leticia", "Nelsen" ),
				new Name( "Martin", "Newkirk" ), new Name( "Kristin", "Nicholls" ), new Name( "Patsy", "Nichols" ), new Name( "Kristine", "Nicholson" ),
				new Name( "Elizabeth", "Nickles" ), new Name( "Deborah", "Nicolas" ), new Name( "Craig", "Nielson" ), new Name( "Jacquelyn", "Nolasco" ),
				new Name( "Peter", "Nordstrom" ), new Name( "Christine", "Noriega" ), new Name( "Pam", "Northrup" ), new Name( "Faye", "Nowicki" ),
				new Name( "Margie", "Nowicki" ), new Name( "Charles", "Null" ), new Name( "Monique", "Nunley" ), new Name( "Madeline", "Nutter" ),
				new Name( "Dale", "Oakes" ), new Name( "Roy", "Oberg" ), new Name( "Mark", "Odom" ), new Name( "Velma", "Oh" ), new Name( "Stanley", "Ojeda" ),
				new Name( "George", "Oliver" ), new Name( "Jonathan", "Ordonez" ), new Name( "Audrey", "Ortego" ), new Name( "Ronald", "Osgood" ),
				new Name( "Stephen", "Oster" ), new Name( "Rachel", "Otero" ), new Name( "Catherine", "Oubre" ), new Name( "Peggy", "Oyler" ),
				new Name( "Martin", "Packard" ), new Name( "Nancy", "Paddock" ), new Name( "Carole", "Pagano" ), new Name( "Beth", "Palmer" ),
				new Name( "Holly", "Paradis" ), new Name( "Gloria", "Parisi" ), new Name( "Lynda", "Parker" ), new Name( "Jonathan", "Parker" ),
				new Name( "Marilyn", "Parkhurst" ), new Name( "Jeremy", "Parkman" ), new Name( "Anthony", "Parrett" ), new Name( "Emily", "Parsons" ),
				new Name( "Arthur", "Pasquale" ), new Name( "Arlene", "Pass" ), new Name( "Terry", "Paulk" ), new Name( "Jonathan", "Paynter" ),
				new Name( "Ronald", "Paynter" ), new Name( "Patty", "Paynter" ), new Name( "Alison", "Peck" ), new Name( "Jose", "Pedersen" ),
				new Name( "Karen", "Pedigo" ), new Name( "Lawrence", "Pedroza" ), new Name( "Edward", "Peery" ), new Name( "Alan", "Pegram" ),
				new Name( "Della", "Pendley" ), new Name( "Allison", "Penner" ), new Name( "Bobby", "Pennington" ), new Name( "Leah", "Penny" ),
				new Name( "Priscilla", "Perrin" ), new Name( "Jenny", "Peters" ), new Name( "Edward", "Peterson" ), new Name( "George", "Petro" ),
				new Name( "Randy", "Peyton" ), new Name( "Beth", "Pham" ), new Name( "Brandi", "Phan" ), new Name( "Louis", "Philpott" ),
				new Name( "Flora", "Pichardo" ), new Name( "Jonathan", "Pickett" ), new Name( "Manuel", "Pifer" ), new Name( "Scott", "Pink" ),
				new Name( "Arthur", "Place" ), new Name( "Denise", "Plascencia" ), new Name( "Mike", "Pless" ), new Name( "Dolores", "Poindexter" ),
				new Name( "Sean", "Poirier" ), new Name( "Dennis", "Polk" ), new Name( "Valerie", "Polk" ), new Name( "Debra", "Polson" ),
				new Name( "Jeremy", "Porch" ), new Name( "Edith", "Poteat" ), new Name( "Jimmy", "Prescott" ), new Name( "Pam", "Pridgen" ),
				new Name( "George", "Primm" ), new Name( "Eva", "Pritchard" ), new Name( "Daisy", "Proctor" ), new Name( "David", "Province" ),
				new Name( "Jeffrey", "Purdy" ), new Name( "Jacqueline", "Putnam" ), new Name( "Claudia", "Pye" ), new Name( "Isabel", "Quan" ),
				new Name( "Bernice", "Quezada" ), new Name( "Paul", "Quigley" ), new Name( "Keith", "Rader" ), new Name( "Yvonne", "Rae" ),
				new Name( "Lillian", "Railey" ), new Name( "Jerry", "Ramsay" ), new Name( "Lawrence", "Randle" ), new Name( "Donald", "Ranson" ),
				new Name( "Elizabeth", "Rathbone" ), new Name( "Jennifer", "Rauch" ), new Name( "Henry", "Razo" ), new Name( "Holly", "Read" ),
				new Name( "Glenn", "Read" ), new Name( "Edna", "Rego" ), new Name( "Christy", "Reinhart" ), new Name( "Danny", "Reinhart" ),
				new Name( "Dale", "Rendon" ), new Name( "Gina", "Revis" ), new Name( "Joanne", "Reynolds" ), new Name( "Beatrice", "Reynoso" ),
				new Name( "Shelley", "Reynoso" ), new Name( "Thomas", "Ricci" ), new Name( "David", "Ricciardi" ), new Name( "Lola", "Richter" ),
				new Name( "Faye", "Ridenhour" ), new Name( "Susie", "Riendeau" ), new Name( "Joshua", "Rigney" ), new Name( "Veronica", "Rioux" ),
				new Name( "Georgia", "Risley" ), new Name( "Crystal", "Risner" ), new Name( "Jose", "Robb" ), new Name( "Jeannette", "Robey" ),
				new Name( "Cheryl", "Robichaux" ), new Name( "Erica", "Robichaux" ), new Name( "Barbara", "Robinette" ), new Name( "Mildred", "Robinson" ),
				new Name( "Marianne", "Rochelle" ), new Name( "Emma", "Rodarte" ), new Name( "Nicholas", "Rogan" ), new Name( "Mike", "Romero" ),
				new Name( "Donald", "Roque" ), new Name( "Misty", "Rosenblatt" ), new Name( "Joyce", "Routh" ), new Name( "Penny", "Royston" ),
				new Name( "Gloria", "Rubin" ), new Name( "Eugene", "Ruble" ), new Name( "Lena", "Rude" ), new Name( "Phillip", "Rude" ), new Name( "Glenn", "Rule" ),
				new Name( "Stephen", "Runnels" ), new Name( "Brian", "Ryals" ), new Name( "Carolyn", "Saito" ), new Name( "Craig", "Sallee" ),
				new Name( "Alison", "Salyers" ), new Name( "Hazel", "Sample" ), new Name( "Todd", "Sample" ), new Name( "Terry", "Sander" ),
				new Name( "Harold", "Santacruz" ), new Name( "Jose", "Santillan" ), new Name( "Donald", "Sato" ), new Name( "Paul", "Satterwhite" ),
				new Name( "Harriet", "Satterwhite" ), new Name( "Dorothy", "Saucedo" ), new Name( "Shawn", "Scanlan" ), new Name( "Sherri", "Scarberry" ),
				new Name( "Nathan", "Schaub" ), new Name( "Jonathan", "Scherer" ), new Name( "Justin", "Scherer" ), new Name( "Natasha", "Schindler" ),
				new Name( "Patty", "Schneider" ), new Name( "Luis", "Schofield" ), new Name( "Larry", "Scholz" ), new Name( "Cynthia", "Schreiber" ),
				new Name( "Wilma", "Schroeder" ), new Name( "Alison", "Schwarz" ), new Name( "Diana", "Scruggs" ), new Name( "Margaret", "Sealey" ),
				new Name( "Timothy", "Seaman" ), new Name( "Craig", "Seger" ), new Name( "Chad", "Sell" ), new Name( "Cassandra", "Seltzer" ),
				new Name( "Janet", "Senn" ), new Name( "Emily", "Serrato" ), new Name( "Timothy", "Sevigny" ), new Name( "Roy", "Sewell" ),
				new Name( "Ruby", "Shah" ), new Name( "Timothy", "Shane" ), new Name( "Anthony", "Shanks" ), new Name( "Terri", "Shattuck" ),
				new Name( "Norman", "Shedd" ), new Name( "Albert", "Sheehan" ), new Name( "Stella", "Shephard" ), new Name( "Jason", "Sherrill" ),
				new Name( "Steve", "Sherrod" ), new Name( "Craig", "Shoffner" ), new Name( "Sherri", "Shrum" ), new Name( "Janie", "Shultz" ),
				new Name( "Jack", "Shumaker" ), new Name( "Manuel", "Shumway" ), new Name( "Elsie", "Sievers" ), new Name( "Genevieve", "Simmon" ),
				new Name( "Kathleen", "Simoneau" ), new Name( "Katherine", "Simoneaux" ), new Name( "Melissa", "Simonton" ), new Name( "Geraldine", "Siu" ),
				new Name( "Paul", "Slayton" ), new Name( "Joshua", "Sloat" ), new Name( "Celia", "Smalley" ), new Name( "Minnie", "Smalls" ),
				new Name( "Blanche", "Smithson" ), new Name( "Walter", "Snow" ), new Name( "Kayla", "Soucy" ), new Name( "Roberta", "Sousa" ),
				new Name( "Roger", "South" ), new Name( "Rita", "Southwick" ), new Name( "Alma", "Spector" ), new Name( "Alan", "Speed" ),
				new Name( "Jose", "Spitzer" ), new Name( "Felicia", "Springs" ), new Name( "Beth", "Squires" ), new Name( "Lois", "Stallings" ),
				new Name( "Misty", "Stambaugh" ), new Name( "Erica", "Stamp" ), new Name( "Juana", "Standifer" ), new Name( "Charles", "Stanley" ),
				new Name( "Naomi", "Starks" ), new Name( "Henry", "Staub" ), new Name( "Monica", "Steele" ), new Name( "Becky", "Steinbach" ),
				new Name( "Ronald", "Steinbach" ), new Name( "Dennis", "Stephenson" ), new Name( "Ella", "Sterling" ), new Name( "Christopher", "Still" ),
				new Name( "Raymond", "Stocker" ), new Name( "Minnie", "Stoltz" ), new Name( "Nellie", "Stover" ), new Name( "Edward", "Strader" ),
				new Name( "Billy", "Street" ), new Name( "Bertha", "Strickler" ), new Name( "Michelle", "Stroup" ), new Name( "Pamela", "Stubbs" ),
				new Name( "George", "Suarez" ), new Name( "Jesse", "Suarez" ), new Name( "Ryan", "Suh" ), new Name( "Bertha", "Surface" ),
				new Name( "Jeff", "Sussman" ), new Name( "Jeremy", "Sutter" ), new Name( "Larry", "Suzuki" ), new Name( "Colleen", "Sweeney" ),
				new Name( "Adam", "Sweet" ), new Name( "Gina", "Switzer" ), new Name( "Stephen", "Taber" ), new Name( "Florence", "Taber" ),
				new Name( "Sean", "Tackett" ), new Name( "Melody", "Talbert" ), new Name( "Jeff", "Talley" ), new Name( "Tonya", "Tam" ),
				new Name( "Veronica", "Tankersley" ), new Name( "Harry", "Tarr" ), new Name( "Larry", "Tart" ), new Name( "Jo", "Taveras" ),
				new Name( "Toni", "Teague" ), new Name( "Jimmy", "Testerman" ), new Name( "Kristi", "Teter" ), new Name( "Samuel", "Thacker" ),
				new Name( "Christopher", "Thibodeau" ), new Name( "Steven", "Thorn" ), new Name( "Lynda", "Tijerina" ), new Name( "Arlene", "Tillery" ),
				new Name( "Geraldine", "Tilley" ), new Name( "Vivian", "Tillotson" ), new Name( "Douglas", "Timberlake" ), new Name( "Lawrence", "Tipton" ),
				new Name( "Craig", "Tisdale" ), new Name( "Cynthia", "To" ), new Name( "Ralph", "Tobin" ), new Name( "Dianne", "Toland" ),
				new Name( "Geneva", "Toms" ), new Name( "Hannah", "Toner" ), new Name( "Terry", "Torrey" ), new Name( "Matthew", "Trainor" ),
				new Name( "George", "Trapp" ), new Name( "Audrey", "Trevino" ), new Name( "Nora", "Trombley" ), new Name( "Dennis", "Trull" ),
				new Name( "Madeline", "Truong" ), new Name( "Gerald", "Tucker" ), new Name( "Benjamin", "Tyler" ), new Name( "Brandi", "Tyree" ),
				new Name( "Roger", "Urban" ), new Name( "Craig", "Vaccaro" ), new Name( "Sherry", "Valerio" ), new Name( "Raymond", "Valverde" ),
				new Name( "Martin", "Vandusen" ), new Name( "Joshua", "Vanlandingham" ), new Name( "Tonya", "Vannoy" ), new Name( "Allison", "Vela" ),
				new Name( "Edward", "Velazquez" ), new Name( "Timothy", "Ventura" ), new Name( "Isabel", "Verret" ), new Name( "Eva", "Vest" ),
				new Name( "Norman", "Victor" ), new Name( "Cassandra", "Vierra" ), new Name( "Norman", "Vigil" ), new Name( "Bryan", "Villalba" ),
				new Name( "Justin", "Voigt" ), new Name( "Adam", "Vue" ), new Name( "Tanya", "Waddell" ), new Name( "Wilma", "Wagstaff" ),
				new Name( "Ana", "Wagstaff" ), new Name( "Jeffrey", "Waldman" ), new Name( "Irma", "Wallen" ), new Name( "Jeffrey", "Walther" ),
				new Name( "Justin", "Washburn" ), new Name( "Victor", "Watford" ), new Name( "Carl", "Weatherspoon" ), new Name( "Carl", "Weber" ),
				new Name( "Henry", "Weddle" ), new Name( "Willie", "Weiland" ), new Name( "Sharon", "Weis" ), new Name( "Beulah", "Welker" ),
				new Name( "Colleen", "Wellington" ), new Name( "Mike", "Wendt" ), new Name( "Dawn", "Westbrooks" ), new Name( "Angie", "Westcott" ),
				new Name( "Judith", "Westcott" ), new Name( "Sheri", "Western" ), new Name( "Howard", "Western" ), new Name( "Henry", "Westfall" ),
				new Name( "Joseph", "Wetzel" ), new Name( "Willie", "Whisenant" ), new Name( "Kristi", "Wick" ), new Name( "Daisy", "Wilford" ),
				new Name( "Alison", "Wilkes" ), new Name( "Jeff", "Wilmot" ), new Name( "Douglas", "Wilmot" ), new Name( "Toni", "Wine" ),
				new Name( "Delores", "Winslow" ), new Name( "Lillian", "Witkowski" ), new Name( "Peter", "Witte" ), new Name( "Jerry", "Wolfe" ),
				new Name( "Vivian", "Wolford" ), new Name( "Willie", "Womack" ), new Name( "Leticia", "Woody" ), new Name( "Rodney", "Woolf" ),
				new Name( "Sally", "Worthy" ), new Name( "Bryan", "Worthy" ), new Name( "Judith", "Wright" ), new Name( "Victor", "Yager" ),
				new Name( "Harriet", "Ybarra" ), new Name( "Bryan", "Young" ), new Name( "Roberta", "Younger" ), new Name( "Marcia", "Zachary" ),
				new Name( "Peter", "Zielinski" ), new Name( "Brenda", "Zimmer" ), new Name( "Tamara", "Zimmermann" )
			};

		#endregion

		private readonly string first;
		private readonly string middle;
		private readonly string last;

		/// <summary>
		/// The person's first name.
		/// </summary>
		public string First { get { return first; } }

		/// <summary>
		/// The person's middle name or initial.
		/// </summary>
		public string Middle { get { return middle; } }

		/// <summary>
		/// The person's last name.
		/// </summary>
		public string Last { get { return last; } }

		/// <summary>
		/// The person's full name (not directory style).
		/// </summary>
		public string FullName { get { return first.ConcatenateWithSpace( middle ).ConcatenateWithSpace( last ); } }

		/// <summary>
		/// An email address a person with this name might have.
		/// </summary>
		public string EmailAddress { get { return ( first + "." + last + "@redstapler.biz" ).ToLower(); } }

		private Name( string first, string last ): this( first, "", last ) {}

		private Name( string first, string middle, string last ) {
			if( first.Length == 0 || last.Length == 0 )
				throw new ApplicationException( "The first and last name cannot be empty." );

			this.first = first;
			this.middle = middle;
			this.last = last;
		}

		[ ThreadStatic ]
		private static IEnumerator<Name>? scrambledNamesPerThread;

		private static IEnumerator<Name> scrambledNames => scrambledNamesPerThread ??= rawNames.Scramble().GetEnumerator();

		// NOTE: Move the random generation portion of this class into a RandomNameGenerator class. Stop using thread static, and have clients create an instance of the RandomNameGenerator,
		// which will basically be a "GetRandomNames( totalNumberOfNames ).GetEnumerator()". We may then want to pass RandomNameGenerators into web testing methods for convenience.

		/// <summary>
		/// Gets a random name. Each random name is guaranteed to be distinct for this thread (which, in ASP.NET, doesn't mean very much)
		/// until the maximum number of names is requested (currently 1086).
		/// </summary>
		public static Name GetRandomName() {
			if( !scrambledNames.MoveNext() ) {
				scrambledNames.Reset();
				scrambledNames.MoveNext();
			}
			return scrambledNames.Current;
		}
	}
}