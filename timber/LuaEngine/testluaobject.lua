testluaobject = {}

testluaobject.MyValue = 5



-- UNSEEN CODE --

function testluaobject:process()
	--Print("Process works!")
end

-- For testing purposes, this is getting run every 5 seconds
function testluaobject:testfunc()
	--local parent = self:get_node("../../CustomScriptManager")
	--for i=1,10 do 
	myvariable = false
	WaitForSeconds(1)
	SetDestination(self,self.x-3, self.z)
	--Hurt(self, 10)
	--Print("I am at x-position: "..self.x)
	WaitForSeconds(2)
	
	--Print("Closest Actor"..GetFarthestActor(self).object_name)
	
	local test = GetFarthestActor(self)
	--PrintNameOfActor(test)
	
	SetDestination(self,self.x+3, self.z)
	--Hurt(self, 10)
	--parent.testData = ""..i;
	--Print("Completed Ready")
	--end
end
